using System;
using System.Collections.Generic;
using System.Text;

namespace Server.CursedSoulsContent
{
    public partial class PGTerritoryStoneRegion : Region
    {
        private readonly GuildTerritoryStone _stoneGuild;

        public GuildTerritoryStone Stone => _stoneGuild;

        public PGTerritoryStoneRegion(GuildTerritoryStone stone, string name, Map map, int priority, params Rectangle3D[] area)
            : base(name, map, priority, area)
        {
            _stoneGuild = stone;
        }

        public override void OnEnter(Mobile m)
        {
            base.OnEnter(m);

            if (_stoneGuild == null || _stoneGuild.Deleted)
                return;

            _stoneGuild.OnMobileEnterRegion(m);
        }

        public override void OnExit(Mobile m)
        {
            base.OnExit(m);

            if (_stoneGuild == null || _stoneGuild.Deleted)
                return;

            _stoneGuild.OnMobileExitRegion(m);
        }
    }
}
