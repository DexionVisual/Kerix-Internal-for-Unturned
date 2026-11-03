/*
 * KerixLoader - native mono bootstrap for Unturned.
 *
 * Injected into Unturned.exe (see kerix.c). Waits for the game's mono runtime,
 * grabs the root domain, then schedules Kerix.Loader.Init() to run on the
 * game's MAIN thread via a one-shot WH_GETMESSAGE hook. The main thread pumps
 * messages every frame, so the hook fires almost immediately. All Unity API
 * calls happen from the main thread - no thread-guard crashes.
 *
 * Reads Kerix.dll from the game root (directory of Unturned.exe).
 * Diagnostics go to kerix_loader.log in the game root.
 */

#include <windows.h>
#include <tlhelp32.h>
#include <stdio.h>

/* ---- mono exports ------------------------------------------------------- */

typedef void* (*fn_get_root_domain)(void);
typedef void* (*fn_thread_attach)(void*);
typedef void* (*fn_domain_assembly_open)(void*, const char*);
typedef void* (*fn_assembly_get_image)(void*);
typedef void* (*fn_class_from_name)(void*, const char*, const char*);
typedef void* (*fn_class_get_method_from_name)(void*, const char*, int);
typedef void* (*fn_runtime_invoke)(void*, void*, void**, void**);

static fn_get_root_domain            p_get_root_domain;
static fn_thread_attach              p_thread_attach;
static fn_domain_assembly_open       p_domain_assembly_open;
static fn_assembly_get_image         p_assembly_get_image;
static fn_class_from_name            p_class_from_name;
static fn_class_get_method_from_name p_class_get_method_from_name;
static fn_runtime_invoke             p_runtime_invoke;

static void* g_domain;
static HHOOK g_hook;
static volatile LONG g_booted;

/* ---- logging ------------------------------------------------------------ */

static void log_line(const char* msg)
{
    char path[MAX_PATH];
    char line[512];
    SYSTEMTIME st;
    HANDLE f;
    DWORD written;
    int n;

    GetModuleFileNameA(NULL, path, MAX_PATH); /* Unturned.exe */
    {
        char* slash = strrchr(path, '\\');
        if (slash) *(slash + 1) = '\0';
    }
    lstrcatA(path, "kerix_loader.log");

    GetLocalTime(&st);
    n = _snprintf(line, sizeof(line) - 2,
                  "[%02u:%02u:%02u] %s\r\n",
                  st.wHour, st.wMinute, st.wSecond, msg);
    if (n <= 0) return;

    f = CreateFileA(path, FILE_APPEND_DATA, FILE_SHARE_READ | FILE_SHARE_WRITE,
                    NULL, OPEN_ALWAYS, FILE_ATTRIBUTE_NORMAL, NULL);
    if (f == INVALID_HANDLE_VALUE) return;
    WriteFile(f, line, (DWORD)n, &written, NULL);
    CloseHandle(f);
}

/* ---- mono bootstrap ------------------------------------------------------ */

static int resolve_mono(HMODULE mono)
{
    struct { const char* name; FARPROC* out; } need[] = {
        { "mono_get_root_domain",           (FARPROC*)&p_get_root_domain },
        { "mono_thread_attach",             (FARPROC*)&p_thread_attach },
        { "mono_domain_assembly_open",      (FARPROC*)&p_domain_assembly_open },
        { "mono_assembly_get_image",        (FARPROC*)&p_assembly_get_image },
        { "mono_class_from_name",           (FARPROC*)&p_class_from_name },
        { "mono_class_get_method_from_name",(FARPROC*)&p_class_get_method_from_name },
        { "mono_runtime_invoke",            (FARPROC*)&p_runtime_invoke },
    };
    int i;
    int ok = 1;

    for (i = 0; i < (int)(sizeof(need) / sizeof(need[0])); i++)
    {
        *need[i].out = GetProcAddress(mono, need[i].name);
        if (!*need[i].out)
        {
            log_line(need[i].name);
            log_line("  ^ export missing");
            ok = 0;
        }
    }
    return ok;
}

/* Loads the managed cheat assembly and invokes Kerix.Loader.Init().
 * MUST run on the game main thread. */
static void run_managed(void)
{
    char path[MAX_PATH];
    char msg[256];
    void* assembly;
    void* image;
    void* klass;
    void* method;

    GetModuleFileNameA(NULL, path, MAX_PATH);
    {
        char* slash = strrchr(path, '\\');
        if (slash) *(slash + 1) = '\0';
    }
    lstrcatA(path, "Kerix.dll");

    p_thread_attach(g_domain);

    assembly = p_domain_assembly_open(g_domain, path);
    if (!assembly)
    {
        log_line("mono_domain_assembly_open failed for Kerix.dll");
        return;
    }
    image  = p_assembly_get_image(assembly);
    klass  = p_class_from_name(image, "Kerix", "Loader");
    if (!klass)
    {
        log_line("class Kerix.Loader not found");
        return;
    }
    method = p_class_get_method_from_name(klass, "Init", 0);
    if (!method)
    {
        log_line("method Kerix.Loader.Init not found");
        return;
    }
    p_runtime_invoke(method, NULL, NULL, NULL);

    _snprintf(msg, sizeof(msg) - 1, "Kerix.Loader.Init invoked on main thread");
    log_line(msg);
}

static LRESULT CALLBACK msg_hook(int code, WPARAM wparam, LPARAM lparam)
{
    if (code >= 0 && !g_booted && g_domain)
    {
        g_booted = 1;
        run_managed();
        if (g_hook) { UnhookWindowsHookEx(g_hook); }
        g_hook = NULL;
    }
    /* hook is being removed; forward with a NULL handle is fine */
    return CallNextHookEx(NULL, code, wparam, lparam);
}

/* ---- main thread discovery ---------------------------------------------- */

struct find_ctx { DWORD pid; DWORD tid; };

static BOOL CALLBACK enum_cb(HWND hwnd, LPARAM lparam)
{
    struct find_ctx* ctx = (struct find_ctx*)lparam;
    DWORD pid = 0;
    DWORD tid = GetWindowThreadProcessId(hwnd, &pid);

    if (tid && pid == ctx->pid && IsWindowVisible(hwnd))
    {
        ctx->tid = tid;
        return FALSE; /* stop */
    }
    return TRUE;
}

static DWORD find_main_thread(void)
{
    struct find_ctx ctx;
    HANDLE snap;
    THREADENTRY32 te;

    ctx.pid = GetCurrentProcessId();
    ctx.tid = 0;

    /* preferred: the thread owning the game's top-level window */
    EnumWindows(enum_cb, (LPARAM)&ctx);
    if (ctx.tid) return ctx.tid;

    /* fallback: Unity window class */
    {
        HWND w = FindWindowW(L"UnityWndClass", NULL);
        DWORD pid = 0;
        DWORD tid;
        if (w)
        {
            tid = GetWindowThreadProcessId(w, &pid);
            if (tid && pid == ctx.pid) return tid;
        }
    }

    /* last resort: oldest thread of the process */
    snap = CreateToolhelp32Snapshot(TH32CS_SNAPTHREAD, 0);
    if (snap != INVALID_HANDLE_VALUE)
    {
        te.dwSize = sizeof(te);
        if (Thread32First(snap, &te))
        {
            do
            {
                if (te.th32OwnerProcessID == ctx.pid)
                {
                    ctx.tid = te.th32ThreadID;
                    break;
                }
            } while (Thread32Next(snap, &te));
        }
        CloseHandle(snap);
    }
    return ctx.tid;
}

/* ---- bootstrap thread ----------------------------------------------------- */

static DWORD WINAPI boot_thread(LPVOID arg)
{
    HMODULE mono = NULL;
    DWORD tid;
    int i;

    (void)arg;
    log_line("KerixLoader bootstrap started");

    for (i = 0; i < 600 && !mono; i++)
    {
        mono = GetModuleHandleA("mono-2.0-bdwgc.dll");
        if (!mono) Sleep(250);
    }
    if (!mono) { log_line("mono-2.0-bdwgc.dll never loaded"); return 1; }
    if (!resolve_mono(mono)) { log_line("mono export resolution failed"); return 1; }

    for (i = 0; i < 600; i++)
    {
        g_domain = p_get_root_domain();
        if (g_domain) break;
        Sleep(250);
    }
    if (!g_domain) { log_line("root domain never appeared"); return 1; }
    log_line("mono root domain acquired");

    /* main thread must already be pumping messages */
    tid = 0;
    for (i = 0; i < 240 && !tid; i++)
    {
        tid = find_main_thread();
        if (!tid) Sleep(250);
    }
    if (!tid) { log_line("main thread not found"); return 1; }

    g_hook = SetWindowsHookExW(WH_GETMESSAGE, msg_hook, NULL, tid);
    if (!g_hook)
    {
        log_line("SetWindowsHookEx failed");
        return 1;
    }
    log_line("WH_GETMESSAGE armed on main thread, waiting for pump");

    /* if the game somehow doesn't pump, retry arming on other threads later;
       normally the first message does it */
    for (i = 0; i < 60 && !g_booted; i++) Sleep(500);
    if (!g_booted) log_line("hook did not fire yet (still waiting)");
    return 0;
}

BOOL WINAPI DllMain(HINSTANCE hinst, DWORD reason, LPVOID reserved)
{
    HANDLE t;
    (void)hinst; (void)reserved;
    if (reason == DLL_PROCESS_ATTACH)
    {
        DisableThreadLibraryCalls(hinst);
        t = CreateThread(NULL, 0, boot_thread, NULL, 0, NULL);
        if (t) CloseHandle(t);
    }
    return TRUE;
}
