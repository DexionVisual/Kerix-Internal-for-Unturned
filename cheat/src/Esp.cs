using System.Collections.Generic;
using System.Reflection;
using SDG.Unturned;
using Steamworks;
using UnityEngine;
namespace Kerix
{
    public static class Esp
    {
        public static bool Enabled = true;
        public static bool Box = true;
        public static bool Skeleton = true;
        public static bool Name = true;
        public static bool Snaplines;
        public static bool Zombies = true;
        public static bool Vehicles = true;
        public static bool Items;
        public static Color PlayerColor = Ui.Royal;
        public static Color ZombieColor = new Color(1f, 0.32f, 0.32f);
        public static Color VehicleColor = new Color(0.3f, 0.88f, 1f);
        private const float MAX_PLAYER_D = 400f;
        private const float MAX_ZOMBIE_D = 200f;
        private const float MAX_VEHICLE_D = 300f;
        private const float MAX_ITEM_D = 80f;
        private const int MAX_ITEMS_SHOWN = 120;
        private static Material _lineMat;
        private static FieldInfo _zombieSkeleton;
        private static readonly Dictionary<Player, Transform[]> _bones = new Dictionary<Player, Transform[]>();
        private static readonly Dictionary<Zombie, Transform[]> _zbones = new Dictionary<Zombie, Transform[]>();
        private static float _cacheNext;
        private static readonly List<Vector3> _linePts = new List<Vector3>(512);
        private static readonly List<Color> _lineCols = new List<Color>(512);
        private static Material LineMat
        {
            get
            {
                if (_lineMat == null)
                {
                    Shader sh = Shader.Find("Sprites/Default");
                    if (sh != null) _lineMat = new Material(sh);
                }
                return _lineMat;
            }
        }
        private static Transform[] GetBones(Transform root)
        {
            if (root == null) return null;
            Transform[] all = root.GetComponentsInChildren<Transform>(true);
            List<Transform> list = new List<Transform>(all.Length);
            for (int i = 1; i < all.Length; i++)
            {
                string n = all[i].name;
                if (n.Contains("Finger") || n.Contains("Toe") || n.Contains("Thumb")) continue;
                list.Add(all[i]);
            }
            return list.ToArray();
        }
        private static Transform[] PlayerBones(Player p)
        {
            if (Time.realtimeSinceStartup >= _cacheNext) { _bones.Clear(); _zbones.Clear(); _cacheNext = Time.realtimeSinceStartup + 3f; }
            Transform[] b;
            if (_bones.TryGetValue(p, out b) && b != null) return b;
            try
            {
                b = GetBones(p.animator.thirdSkeleton);
                _bones[p] = b;
                return b;
            }
            catch { return null; }
        }
        private static Transform[] ZombieBones(Zombie z)
        {
            if (Time.realtimeSinceStartup >= _cacheNext) { _bones.Clear(); _zbones.Clear(); _cacheNext = Time.realtimeSinceStartup + 3f; }
            Transform[] b;
            if (_zbones.TryGetValue(z, out b) && b != null) return b;
            try
            {
                if (_zombieSkeleton == null)
                    _zombieSkeleton = typeof(Zombie).GetField("skeleton", BindingFlags.NonPublic | BindingFlags.Instance);
                b = GetBones(_zombieSkeleton.GetValue(z) as Transform);
                _zbones[z] = b;
                return b;
            }
            catch { return null; }
        }
        private static float HeightOf(Player p)
        {
            try
            {
                switch (p.stance.stance)
                {
                    case EPlayerStance.PRONE: return 0.8f;
                    case EPlayerStance.CROUCH: return 1.5f;
                    case EPlayerStance.SITTING: return 1.4f;
                    case EPlayerStance.DRIVING: return 1.5f;
                    default: return 1.95f;
                }
            }
            catch { return 1.95f; }
        }
        private static void ThinBox(float x, float y, float w, float h, Color c)
        {
            Ui.RectFill(new Rect(x, y, w, 1f), c);
            Ui.RectFill(new Rect(x, y + h - 1f, w, 1f), c);
            Ui.RectFill(new Rect(x, y, 1f, h), c);
            Ui.RectFill(new Rect(x + w - 1f, y, 1f, h), c);
        }
        private static void SmallLabel(float x, float y, string text, Color c)
        {
            GUIStyle ls = Ui.Style(9, bold: true, anchor: TextAnchor.UpperLeft);
            Ui.OutlinedLabel(new Rect(x, y, 160f, 13f), text, text, ls, c);
        }
        public static void Draw()
        {
            if (Event.current.type != EventType.Repaint) return;
            if (!(Enabled || Zombies || Vehicles || Items)) return;
            Camera cam = Camera.main;
            if (cam == null) return;
            Player local = Player.LocalPlayer;
            Vector3 localPos = local != null ? local.transform.position : cam.transform.position;
            float sh = Screen.height;
            _linePts.Clear();
            _lineCols.Clear();
            if (Enabled) GatherPlayers(cam, local, localPos, sh);
            if (Zombies) GatherZombies(cam, localPos, sh);
            FlushLines(sh);
            if (Enabled) DrawPlayerGui(cam, local, localPos, sh);
            if (Zombies) DrawZombieGui(cam, localPos, sh);
            if (Vehicles) DrawVehicles(cam, localPos, sh);
            if (Items) DrawItems(cam, localPos, sh);
        }
        private static void GatherPlayers(Camera cam, Player local, Vector3 localPos, float sh)
        {
            List<SteamPlayer> clients = Provider.clients;
            if (clients == null) return;
            for (int i = 0; i < clients.Count; i++)
            {
                SteamPlayer sp = clients[i];
                if (sp == null || sp.player == null || sp.player == local) continue;
                Player p = sp.player;
                try
                {
                    if (p.life == null || p.life.isDead) continue;
                    Vector3 feet = p.transform.position;
                    Vector3 s = cam.WorldToScreenPoint(feet);
                    if (s.z <= 0.05f) continue;
                    float d = Vector3.Distance(localPos, feet);
                    if (d > MAX_PLAYER_D) continue;
                    Color c = PlayerColor;
                    if (Skeleton)
                    {
                        Transform[] b = PlayerBones(p);
                        if (b != null)
                        {
                            for (int j = 0; j < b.Length; j++)
                            {
                                Transform bone = b[j];
                                Transform par = bone.parent;
                                if (par == null) continue;
                                if ((bone.position - par.position).sqrMagnitude > 0.49f) continue;
                                Vector3 a = cam.WorldToScreenPoint(bone.position);
                                Vector3 q = cam.WorldToScreenPoint(par.position);
                                if (a.z <= 0.05f || q.z <= 0.05f) continue;
                                _linePts.Add(new Vector3(a.x, sh - a.y, 0f));
                                _linePts.Add(new Vector3(q.x, sh - q.y, 0f));
                                _lineCols.Add(c);
                                _lineCols.Add(c);
                            }
                        }
                    }
                    if (Snaplines)
                    {
                        _linePts.Add(new Vector3(Screen.width * 0.5f, sh, 0f));
                        _linePts.Add(new Vector3(s.x, sh - s.y, 0f));
                        Color sc = c;
                        sc.a = 0.6f;
                        _lineCols.Add(sc);
                        _lineCols.Add(sc);
                    }
                }
                catch { }
            }
        }
        private static void GatherZombies(Camera cam, Vector3 localPos, float sh)
        {
            ZombieRegion[] regions = ZombieManager.regions;
            if (regions == null) return;
            for (int r = 0; r < regions.Length; r++)
            {
                ZombieRegion region = regions[r];
                if (region == null || region.zombies == null) continue;
                for (int i = 0; i < region.zombies.Count; i++)
                {
                    Zombie z = region.zombies[i];
                    if (z == null) continue;
                    try
                    {
                        if (z.isDead || z.transform == null) continue;
                        Vector3 feet = z.transform.position;
                        Vector3 s = cam.WorldToScreenPoint(feet);
                        if (s.z <= 0.05f) continue;
                        float d = Vector3.Distance(localPos, feet);
                        if (d > MAX_ZOMBIE_D) continue;
                        Color c = ZombieColor;
                        if (z.isBoss) c = new Color(1f, 0.72f, 0.2f);
                        else if (z.isMega) c = new Color(1f, 0.45f, 0.1f);
                        if (Skeleton)
                        {
                            Transform[] b = ZombieBones(z);
                            if (b != null)
                            {
                                for (int j = 0; j < b.Length; j++)
                                {
                                    Transform bone = b[j];
                                    Transform par = bone.parent;
                                    if (par == null) continue;
                                    if ((bone.position - par.position).sqrMagnitude > 0.49f) continue;
                                    Vector3 a = cam.WorldToScreenPoint(bone.position);
                                    Vector3 q = cam.WorldToScreenPoint(par.position);
                                    if (a.z <= 0.05f || q.z <= 0.05f) continue;
                                    _linePts.Add(new Vector3(a.x, sh - a.y, 0f));
                                    _linePts.Add(new Vector3(q.x, sh - q.y, 0f));
                                    _lineCols.Add(c);
                                    _lineCols.Add(c);
                                }
                            }
                        }
                    }
                    catch { }
                }
            }
        }
        private static void FlushLines(float sh)
        {
            Material m = LineMat;
            if (m == null || _linePts.Count == 0) return;
            GL.PushMatrix();
            m.SetPass(0);
            GL.LoadPixelMatrix(0f, Screen.width, sh, 0f);
            GL.Begin(GL.LINES);
            for (int i = 0; i < _linePts.Count; i++)
            {
                GL.Color(_lineCols[i]);
                GL.Vertex3(_linePts[i].x, _linePts[i].y, 0f);
            }
            GL.End();
            GL.PopMatrix();
        }
        private static void DrawPlayerGui(Camera cam, Player local, Vector3 localPos, float sh)
        {
            List<SteamPlayer> clients = Provider.clients;
            if (clients == null) return;
            for (int i = 0; i < clients.Count; i++)
            {
                SteamPlayer sp = clients[i];
                if (sp == null || sp.player == null || sp.player == local) continue;
                Player p = sp.player;
                try
                {
                    if (p.life == null || p.life.isDead) continue;
                    Vector3 feet = p.transform.position;
                    Vector3 s = cam.WorldToScreenPoint(feet);
                    if (s.z <= 0.05f) continue;
                    float d = Vector3.Distance(localPos, feet);
                    if (d > MAX_PLAYER_D) continue;
                    Vector3 headS = cam.WorldToScreenPoint(feet + Vector3.up * HeightOf(p));
                    float yF = sh - s.y;
                    float yH = sh - headS.y;
                    float h = yF - yH;
                    if (h < 3f) continue;
                    float w = h * 0.48f;
                    float x = s.x - w * 0.5f;
                    Color c = PlayerColor;
                    if (Box) ThinBox(x, yH, w, h, c);
                    if (Name)
                    {
                        string nm = sp.playerID != null ? sp.playerID.characterName : "?";
                        SmallLabel(x + w + 4f, yH, nm, c);
                    }
                }
                catch { }
            }
        }
        private static void DrawZombieGui(Camera cam, Vector3 localPos, float sh)
        {
            ZombieRegion[] regions = ZombieManager.regions;
            if (regions == null) return;
            for (int r = 0; r < regions.Length; r++)
            {
                ZombieRegion region = regions[r];
                if (region == null || region.zombies == null) continue;
                for (int i = 0; i < region.zombies.Count; i++)
                {
                    Zombie z = region.zombies[i];
                    if (z == null) continue;
                    try
                    {
                        if (z.isDead || z.transform == null) continue;
                        Vector3 feet = z.transform.position;
                        Vector3 s = cam.WorldToScreenPoint(feet);
                        if (s.z <= 0.05f) continue;
                        float d = Vector3.Distance(localPos, feet);
                        if (d > MAX_ZOMBIE_D) continue;
                        Vector3 headS = cam.WorldToScreenPoint(feet + Vector3.up * 1.85f);
                        float yF = sh - s.y;
                        float yH = sh - headS.y;
                        float h = yF - yH;
                        if (h < 3f) continue;
                        float w = h * 0.48f;
                        float x = s.x - w * 0.5f;
                        Color c = ZombieColor;
                        if (z.isBoss) c = new Color(1f, 0.72f, 0.2f);
                        else if (z.isMega) c = new Color(1f, 0.45f, 0.1f);
                        if (Box) ThinBox(x, yH, w, h, c);
                        SmallLabel(x + w + 4f, yH, "Z", c);
                    }
                    catch { }
                }
            }
        }
        private static void DrawVehicles(Camera cam, Vector3 localPos, float sh)
        {
            List<InteractableVehicle> vehicles = VehicleManager.vehicles;
            if (vehicles == null) return;
            for (int i = 0; i < vehicles.Count; i++)
            {
                InteractableVehicle v = vehicles[i];
                if (v == null) continue;
                try
                {
                    if (v.isDead || v.IsChildOfVehicle || v.transform == null) continue;
                    Vector3 pos = v.transform.position;
                    Vector3 s = cam.WorldToScreenPoint(pos);
                    if (s.z <= 0.05f) continue;
                    float d = Vector3.Distance(localPos, pos);
                    if (d > MAX_VEHICLE_D) continue;
                    Vector3 topS = cam.WorldToScreenPoint(pos + Vector3.up * 2.2f);
                    float yF = sh - s.y;
                    float yT = sh - topS.y;
                    float h = yF - yT;
                    if (h < 3f) continue;
                    float w = h * 1.1f;
                    float x = s.x - w * 0.5f;
                    Color c = VehicleColor;
                    if (Box) ThinBox(x, yT, w, h, c);
                    string nm = v.asset != null ? v.asset.vehicleName : "Vehicle";
                    float ty = yT - 14f;
                    if (Name) SmallLabel(x, ty, nm, c);
                    GUIStyle ls = Ui.Style(9, anchor: TextAnchor.UpperLeft);
                    float ly = yF + 2f;
                    if (v.usesHealth && v.asset != null && v.asset.health > 0)
                    {
                        string hp = Mathf.RoundToInt(v.health * 100f / v.asset.health) + "%";
                        Ui.OutlinedLabel(new Rect(x, ly, 60f, 12f), hp, hp, ls,
                            Color.Lerp(new Color(0.92f, 0.2f, 0.2f), new Color(0.3f, 0.95f, 0.4f),
                                Mathf.Clamp01(v.health / (float)v.asset.health)));
                        ly += 12f;
                    }
                    if (v.usesFuel && v.asset != null && v.asset.fuel > 0)
                    {
                        string fu = Mathf.RoundToInt(v.fuel * 100f / v.asset.fuel) + "%";
                        Ui.OutlinedLabel(new Rect(x, ly, 60f, 12f), fu, fu, ls, new Color(0.95f, 0.8f, 0.3f));
                    }
                }
                catch { }
            }
        }
        private static Color RarityColor(EItemRarity rarity)
        {
            switch (rarity)
            {
                case EItemRarity.UNCOMMON: return new Color(0.30f, 0.85f, 0.35f);
                case EItemRarity.RARE: return new Color(0.30f, 0.55f, 1.00f);
                case EItemRarity.EPIC: return new Color(0.68f, 0.35f, 0.98f);
                case EItemRarity.LEGENDARY: return new Color(1.00f, 0.72f, 0.20f);
                case EItemRarity.MYTHICAL: return new Color(1.00f, 0.25f, 0.30f);
                default: return new Color(0.65f, 0.65f, 0.68f);
            }
        }
        private static void DrawItems(Camera cam, Vector3 localPos, float sh)
        {
            ItemRegion[,] regions = ItemManager.regions;
            if (regions == null) return;
            int shown = 0;
            int rx = regions.GetLength(0);
            int ry = regions.GetLength(1);
            for (int x = 0; x < rx && shown < MAX_ITEMS_SHOWN; x++)
            {
                for (int y = 0; y < ry && shown < MAX_ITEMS_SHOWN; y++)
                {
                    ItemRegion region = regions[x, y];
                    if (region == null || region.drops == null) continue;
                    for (int i = 0; i < region.drops.Count && shown < MAX_ITEMS_SHOWN; i++)
                    {
                        ItemDrop jar = region.drops[i];
                        if (jar == null || jar.interactableItem == null || jar.model == null) continue;
                        try
                        {
                            InteractableItem it = jar.interactableItem;
                            if (it.transform == null || it.asset == null) continue;
                            Vector3 pos = it.transform.position;
                            Vector3 s = cam.WorldToScreenPoint(pos + Vector3.up * 0.4f);
                            if (s.z <= 0.05f) continue;
                            float d = Vector3.Distance(localPos, pos);
                            if (d > MAX_ITEM_D) continue;
                            Color c = RarityColor(it.asset.rarity);
                            string nm = it.asset.itemName;
                            Item item = it.item;
                            if (item != null && item.amount > 1)
                                nm += " x" + item.amount;
                            SmallLabel(s.x + 6f, sh - s.y - 8f, nm, c);
                            shown++;
                        }
                        catch { }
                    }
                }
            }
        }
    }
}
