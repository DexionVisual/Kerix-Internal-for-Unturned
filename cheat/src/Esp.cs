using System.Collections.Generic;
using SDG.Unturned;
using UnityEngine;

namespace Kerix
{
    public static class Esp
    {

        public static bool Enabled = true;
        public static bool Box = true;
        public static bool Fill;
        public static bool Health;
        public static bool Name = true;
        public static bool Distance = true;
        public static bool Snaplines;

        public static bool Zombies = true;
        public static bool Vehicles = true;
        public static bool Items;

        private const float MAX_PLAYER_D = 400f;
        private const float MAX_ZOMBIE_D = 150f;
        private const float MAX_VEHICLE_D = 250f;
        private const float MAX_ITEM_D = 80f;
        private const int MAX_ITEMS_SHOWN = 120;

        private static Material _lineMat;
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

        private static void NameDist(float x, float y, string name, float dist, Color color, float dLimit)
        {
            GUIStyle ls = Ui.Style(13, bold: true, anchor: TextAnchor.MiddleLeft);
            if (name != null)
            {
                Ui.OutlinedLabel(new Rect(x, y, 280f, 18f), name, name, ls, color);
                y += 17f;
            }
            if (Distance && dist <= dLimit)
            {
                string ds = ((int)dist) + "m";
                Ui.OutlinedLabel(new Rect(x, y, 280f, 16f), ds, ds, ls, new Color(0.78f, 0.82f, 0.95f));
            }
        }

        private static void BoxAt(Vector3 s, float yHead, float yFeet, Color c, float widthScale)
        {
            float bh = yFeet - yHead;
            if (bh < 3f) return;
            float bw = bh * widthScale;
            float bx = s.x - bw * 0.5f;
            float th = 1.6f;
            Ui.RectFill(new Rect(bx, yHead, bw, th), c);
            Ui.RectFill(new Rect(bx, yFeet - th, bw, th), c);
            Ui.RectFill(new Rect(bx, yHead, th, bh), c);
            Ui.RectFill(new Rect(bx + bw - th, yHead, th, bh), c);
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

            if (Enabled) DrawPlayers(cam, local, localPos, sh);
            if (Zombies) DrawZombies(cam, localPos, sh);
            if (Vehicles) DrawVehicles(cam, localPos, sh);
            if (Items) DrawItems(cam, localPos, sh);
        }

        private static void DrawPlayers(Camera cam, Player local, Vector3 localPos, float sh)
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
                    float yFeet = sh - s.y;
                    float yHead = sh - headS.y;
                    float bh = yFeet - yHead;
                    if (bh < 3f) continue;
                    float bw = bh * 0.48f;
                    float bx = s.x - bw * 0.5f;

                    if (Fill)
                        Ui.RectFill(new Rect(bx, yHead, bw, bh), new Color(0.35f, 0.55f, 1f, 0.10f));

                    if (Box)
                    {
                        Color bc = new Color(1f, 1f, 1f, 0.92f);
                        float th = 1.6f;
                        Ui.RectFill(new Rect(bx, yHead, bw, th), bc);
                        Ui.RectFill(new Rect(bx, yFeet - th, bw, th), bc);
                        Ui.RectFill(new Rect(bx, yHead, th, bh), bc);
                        Ui.RectFill(new Rect(bx + bw - th, yHead, th, bh), bc);
                    }

                    if (Health)
                    {
                        float frac = Mathf.Clamp01(p.life.health / 100f);
                        Color hc = Color.Lerp(new Color(0.92f, 0.16f, 0.16f), new Color(0.22f, 0.9f, 0.32f), frac);
                        Rect back = new Rect(bx - 8f, yHead, 4f, bh);
                        Ui.RectFill(back, new Color(0f, 0f, 0f, 0.55f));
                        Ui.RectFill(new Rect(back.x, yFeet - bh * frac, back.width, bh * frac), hc);
                    }

                    GUIStyle ls = Ui.Style(13, bold: true, anchor: TextAnchor.MiddleLeft);
                    float tx = bx + bw + 7f;
                    float ty = yHead;
                    if (Name)
                    {
                        string nm = sp.playerID != null ? sp.playerID.characterName : "?";
                        Ui.OutlinedLabel(new Rect(tx, ty, 260f, 18f), nm, nm, ls, Color.white);
                        ty += 17f;
                    }
                    if (Distance)
                    {
                        string ds = ((int)d) + "m";
                        Ui.OutlinedLabel(new Rect(tx, ty, 260f, 16f), ds, ds, ls, new Color(0.78f, 0.82f, 0.95f));
                    }
                }
                catch { }
            }

            if (Snaplines)
                DrawSnaplines(cam, sh);
        }

        private static void DrawSnaplines(Camera cam, float sh)
        {
            Material m = LineMat;
            List<SteamPlayer> clients = Provider.clients;
            if (m == null || clients == null) return;

            GL.PushMatrix();
            m.SetPass(0);
            GL.LoadPixelMatrix(0f, Screen.width, sh, 0f);
            GL.Begin(GL.LINES);
            GL.Color(new Color(0.45f, 0.7f, 1f, 0.8f));
            for (int i = 0; i < clients.Count; i++)
            {
                SteamPlayer sp = clients[i];
                if (sp == null || sp.player == null || sp.player == Player.LocalPlayer) continue;
                try
                {
                    if (sp.player.life == null || sp.player.life.isDead) continue;
                    Vector3 s = cam.WorldToScreenPoint(sp.player.transform.position);
                    if (s.z <= 0.05f) continue;
                    GL.Vertex3(Screen.width * 0.5f, sh, 0f);
                    GL.Vertex3(s.x, sh - s.y, 0f);
                }
                catch { }
            }
            GL.End();
            GL.PopMatrix();
        }

        private static void DrawZombies(Camera cam, Vector3 localPos, float sh)
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
                        float yFeet = sh - s.y;
                        float yHead = sh - headS.y;
                        if (yFeet - yHead < 3f) continue;

                        Color c = new Color(1f, 0.25f, 0.25f, 0.9f);
                        string label = "Zombie";
                        if (z.isBoss) { c = new Color(1f, 0.72f, 0.2f, 0.95f); label = "Boss"; }
                        else if (z.isMega) { c = new Color(1f, 0.45f, 0.1f, 0.95f); label = "Mega"; }
                        else if (z.isRadioactive) { c = new Color(0.35f, 1f, 0.35f, 0.9f); label = "Radioactive"; }

                        BoxAt(s, yHead, yFeet, c, 0.48f);
                        NameDist(s.x + (yFeet - yHead) * 0.24f + 6f, yHead, label, d, c, MAX_ZOMBIE_D);
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

                    Vector3 feet = v.transform.position;
                    Vector3 s = cam.WorldToScreenPoint(feet);
                    if (s.z <= 0.05f) continue;

                    float d = Vector3.Distance(localPos, feet);
                    if (d > MAX_VEHICLE_D) continue;

                    Vector3 headS = cam.WorldToScreenPoint(feet + Vector3.up * 2.2f);
                    float yFeet = sh - s.y;
                    float yHead = sh - headS.y;
                    if (yFeet - yHead < 3f) continue;

                    Color c = new Color(0.30f, 0.88f, 1f, 0.9f);
                    BoxAt(s, yHead, yFeet, c, 1.1f);

                    float tx = s.x + (yFeet - yHead) * 0.55f + 6f;
                    string nm = v.asset != null ? v.asset.vehicleName : "Vehicle";
                    if (v.isLocked) nm += " [locked]";
                    NameDist(tx, yHead, nm, d, c, MAX_VEHICLE_D);

                    GUIStyle ls = Ui.Style(12, anchor: TextAnchor.MiddleLeft);
                    float ty = yHead + 34f;
                    if (v.usesHealth && v.asset != null && v.asset.health > 0)
                    {
                        string hp = "HP " + Mathf.RoundToInt(v.health * 100f / v.asset.health) + "%";
                        Ui.OutlinedLabel(new Rect(tx, ty, 280f, 15f), hp, hp, ls,
                            Color.Lerp(new Color(0.92f, 0.2f, 0.2f), new Color(0.3f, 0.95f, 0.4f),
                                Mathf.Clamp01(v.health / (float)v.asset.health)));
                        ty += 15f;
                    }
                    if (v.usesFuel && v.asset != null && v.asset.fuel > 0)
                    {
                        string fu = "Fuel " + Mathf.RoundToInt(v.fuel * 100f / v.asset.fuel) + "%";
                        Ui.OutlinedLabel(new Rect(tx, ty, 280f, 15f), fu, fu, ls, new Color(0.95f, 0.8f, 0.3f));
                    }
                }
                catch { }
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

                            GUIStyle ls = Ui.Style(13, bold: true, anchor: TextAnchor.MiddleLeft);
                            Ui.OutlinedLabel(new Rect(s.x + 8f, Screen.height - s.y - 26f, 280f, 16f), nm, nm, ls, c);
                            if (Distance)
                            {
                                string ds = ((int)d) + "m";
                                Ui.OutlinedLabel(new Rect(s.x + 8f, Screen.height - s.y - 10f, 280f, 14f), ds, ds, ls,
                                    new Color(0.78f, 0.82f, 0.95f));
                            }
                            shown++;
                        }
                        catch { }
                    }
                }
            }
        }
    }
}
