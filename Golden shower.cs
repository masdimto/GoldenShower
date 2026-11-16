using System;
using System.Collections.Generic;
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
        public override System.Version Version => new System.Version(1, 1, 0);
        public override string Author => "CustomPlugin";
        public override string Description => "Proyektil Golden Shower tunggal melesat lurus tanpa cabang.";

        private Dictionary<int, Vector2> serverStraightStreams = new Dictionary<int, Vector2>();

        public GoldenShowerZeroG(Main game) : base(game) { }

        public override void Initialize()
        {
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
                ServerApi.Hooks.GameUpdate.Deregister(this, OnGameUpdate);
            }
            base.Dispose(disposing);
        }

        private void CheckStatusCommand(CommandArgs args)
        {
            args.Player.SendSuccessMessage("[GoldenShowerZeroG] Aktif! Golden Shower menembak 1 garis lurus tunggal.");
        }

        private void OnGameUpdate(EventArgs args)
        {
            for (int p = 0; p < Main.maxProjectiles; p++)
            {
                Projectile proj = Main.projectile[p];

                if (!proj.active || proj.type != ProjectileID.GoldenShowerFriendly)
                {
                    if (serverStraightStreams.ContainsKey(p))
                        serverStraightStreams.Remove(p);
                    continue;
                }

                // 1. Jika ini adalah proyektil milik PLAYER (yang bikin bercabang), matikan dan ganti dengan proyektil SERVER lurus
                if (proj.owner >= 0 && proj.owner < Main.maxPlayers)
                {
                    TSPlayer tsPlayer = TShock.Players[proj.owner];
                    if (tsPlayer == null || !tsPlayer.Active || tsPlayer.TPlayer == null || !tsPlayer.HasPermission("goldenshower.zerog"))
                        continue;

                    Vector2 spawnPos = proj.Center;
                    Vector2 straightVel = proj.velocity * 1.35f;
                    int dmg = proj.damage;
                    float kb = proj.knockBack;

                    // Hapus proyektil asli player agar tidak muncul cabang kedua di layar HP
                    proj.Kill();
                    NetMessage.SendData((int)PacketTypes.ProjectileDestroy, -1, -1, null, p, proj.owner);

                    // Buat proyektil pengganti milik SERVER (owner = 255)
                    int newIdx = Projectile.NewProjectile(
                        Projectile.GetNoneSource(),
                        spawnPos,
                        straightVel,
                        ProjectileID.GoldenShowerFriendly,
                        dmg,
                        kb,
                        Main.maxPlayers
                    );

                    if (newIdx >= 0 && newIdx < Main.maxProjectiles)
                    {
                        Projectile newProj = Main.projectile[newIdx];
                        newProj.friendly = true;
                        newProj.hostile = false;
                        serverStraightStreams[newIdx] = straightVel;
                        NetMessage.SendData((int)PacketTypes.ProjectileNew, -1, -1, null, newIdx);
                    }
                    continue;
                }

                // 2. Pertahankan proyektil milik SERVER (owner = 255) agar tetap lurus tanpa melengkung jatuh
                if (proj.owner == Main.maxPlayers && serverStraightStreams.TryGetValue(p, out Vector2 lockedVel))
                {
                    proj.velocity = lockedVel;
                    if (proj.timeLeft % 3 == 0)
                    {
                        NetMessage.SendData((int)PacketTypes.ProjectileNew, -1, -1, null, p);
                    }
                }
            }
        }
    }
}
