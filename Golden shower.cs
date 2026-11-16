using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using TerrariaApi.Server;
using TShockAPI;

namespace GoldenShowerZeroGPlugin
{
    [ApiVersion(2, 1)]
    public class GoldenShowerZeroG : TerrariaPlugin
    {
        public override string Name => "GoldenShowerZeroG";
        public override System.Version Version => new System.Version(1, 2, 1);
        public override string Author => "CustomPlugin";
        public override string Description => "Mencegat paket proyektil Golden Shower agar hanya muncul 1 garis lurus tunggal.";

        private Dictionary<int, Vector2> straightStreams = new Dictionary<int, Vector2>();

        public GoldenShowerZeroG(Main game) : base(game) { }

        public override void Initialize()
        {
            ServerApi.Hooks.NetGetData.Register(this, OnGetData);
            ServerApi.Hooks.GameUpdate.Register(this, OnGameUpdate);

            Commands.ChatCommands.Add(new Command("goldenshower.zerog", CheckStatusCommand, "gszerog", "gravgs")
            {
                HelpText = "Mengecek status rework Golden Shower Zero-G."
            });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ServerApi.Hooks.NetGetData.Deregister(this, OnGetData);
                ServerApi.Hooks.GameUpdate.Deregister(this, OnGameUpdate);
            }
            base.Dispose(disposing);
        }

        private void CheckStatusCommand(CommandArgs args)
        {
            args.Player.SendSuccessMessage("[GoldenShowerZeroG v1.2.1] Aktif! Paket proyektil Golden Shower dibajak langsung menjadi 1 garis lurus.");
        }

        private void OnGetData(GetDataEventArgs args)
        {
            if (args.Handled || args.MsgID != PacketTypes.ProjectileNew)
                return;

            int plrIdx = args.Msg.whoAmI;
            if (plrIdx < 0 || plrIdx >= Main.maxPlayers)
                return;

            TSPlayer tsPlayer = TShock.Players[plrIdx];
            if (tsPlayer == null || !tsPlayer.Active || !tsPlayer.HasPermission("goldenshower.zerog"))
                return;

            try
            {
                using (MemoryStream ms = new MemoryStream(args.Msg.readBuffer, args.Index, args.Length))
                using (BinaryReader reader = new BinaryReader(ms))
                {
                    short identity = reader.ReadInt16();
                    Vector2 position = new Vector2(reader.ReadSingle(), reader.ReadSingle());
                    Vector2 velocity = new Vector2(reader.ReadSingle(), reader.ReadSingle());
                    byte owner = reader.ReadByte();
                    short type = reader.ReadInt16();

                    if (type == ProjectileID.GoldenShowerFriendly)
                    {
                        // Hentikan paket asli dari HP sebelum disebar ke pemain lain
                        args.Handled = true;

                        Vector2 straightVel = velocity * 1.35f;
                        int dmg = tsPlayer.TPlayer.GetWeaponDamage(tsPlayer.TPlayer.HeldItem);
                        float kb = tsPlayer.TPlayer.HeldItem.knockBack;

                        int projIdx = Projectile.NewProjectile(
                            Projectile.GetNoneSource(),
                            position,
                            straightVel,
                            ProjectileID.GoldenShowerFriendly,
                            dmg,
                            kb,
                            Main.maxPlayers
                        );

                        if (projIdx >= 0 && projIdx < Main.maxProjectiles)
                        {
                            Projectile p = Main.projectile[projIdx];
                            p.friendly = true;
                            p.hostile = false;

                            straightStreams[projIdx] = straightVel;

                            // Kirim proyektil lurus ke semua pemain
                            NetMessage.SendData((int)PacketTypes.ProjectileNew, -1, -1, null, projIdx);
                        }
                    }
                }
            }
            catch
            {
                // Abaikan jika struktur paket berbeda
            }
        }

        private void OnGameUpdate(EventArgs args)
        {
            for (int p = 0; p < Main.maxProjectiles; p++)
            {
                Projectile proj = Main.projectile[p];

                if (!proj.active || proj.type != ProjectileID.GoldenShowerFriendly)
                {
                    if (straightStreams.ContainsKey(p))
                        straightStreams.Remove(p);
                    continue;
                }

                if (straightStreams.TryGetValue(p, out Vector2 lockedVel))
                {
                    proj.velocity = lockedVel;
                    if (proj.timeLeft % 2 == 0)
                    {
                        NetMessage.SendData((int)PacketTypes.ProjectileNew, -1, -1, null, p);
                    }
                }
            }
        }
    }
}
