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
        public override System.Version Version => new System.Version(1, 0, 1);
        public override string Author => "CustomPlugin";
        public override string Description => "Proyektil Golden Shower melesat lurus tanpa gravitasi.";

        private Dictionary<int, Vector2> lockedVelocities = new Dictionary<int, Vector2>();

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
            args.Player.SendSuccessMessage("[GoldenShowerZeroG] Aktif! Golden Shower otomatis menembak lurus tanpa melengkung jatuh.");
        }

        private void OnGameUpdate(EventArgs args)
        {
            for (int p = 0; p < Main.maxProjectiles; p++)
            {
                Projectile proj = Main.projectile[p];

                if (!proj.active || proj.type != ProjectileID.GoldenShowerFriendly)
                {
                    if (lockedVelocities.ContainsKey(p))
                        lockedVelocities.Remove(p);
                    continue;
                }

                int owner = proj.owner;
                if (owner < 0 || owner >= Main.maxPlayers)
                    continue;

                TSPlayer tsPlayer = TShock.Players[owner];
                if (tsPlayer == null || !tsPlayer.Active || tsPlayer.TPlayer == null)
                    continue;

                if (!tsPlayer.HasPermission("goldenshower.zerog"))
                    continue;

                if (!lockedVelocities.ContainsKey(p))
                {
                    if (proj.velocity.LengthSquared() > 0.1f)
                    {
                        lockedVelocities[p] = proj.velocity * 1.35f;
                    }
                    else
                    {
                        continue;
                    }
                }

                proj.velocity = lockedVelocities[p];

                if (proj.timeLeft % 3 == 0)
                {
                    NetMessage.SendData((int)PacketTypes.ProjectileNew, -1, -1, null, p);
                }
            }
        }
    }
}
