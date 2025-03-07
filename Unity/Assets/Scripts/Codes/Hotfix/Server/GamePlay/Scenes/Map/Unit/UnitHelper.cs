﻿using System.Collections.Generic;
using Unity.Mathematics;

namespace ET.Server
{
    [FriendOf(typeof(MoveComponent))]
    [FriendOf(typeof(NumericComponent))]
    public static class UnitHelper
    {
        public static UnitInfo CreateUnitInfo(Unit unit, Unit owner = null)
        {
            UnitInfo unitInfo = new UnitInfo();
            NumericComponent nc = unit.GetComponent<NumericComponent>();
            unitInfo.UnitId = unit.Id;
            unitInfo.ConfigId = unit.ConfigId;
            unitInfo.Type = (int)unit.Type;
            unitInfo.Position = unit.Position;
            unitInfo.Forward = unit.Forward;
            if (owner != null)
            {
                unitInfo.OwnerId = owner.Id;
            }

            MoveComponent moveComponent = unit.GetComponent<MoveComponent>();
            if (moveComponent != null)
            {
                if (!moveComponent.IsArrived())
                {
                    unitInfo.MoveInfo = new MoveInfo() { Points = new List<float3>() };
                    unitInfo.MoveInfo.Points.Add(unit.Position);
                    for (int i = moveComponent.N; i < moveComponent.Targets.Count; ++i)
                    {
                        float3 pos = moveComponent.Targets[i];
                        unitInfo.MoveInfo.Points.Add(pos);
                    }
                }
            }

            if (nc != null)
            {
                unitInfo.KV = new Dictionary<int, long>();
                foreach ((int key, long value) in nc.NumericDic)
                {
                    unitInfo.KV.Add(key, value);
                }
            }

            if (unit.Config?.BornSkills?.Length > 0)
            {
                unitInfo.Skills = new Dictionary<int, int>();
                //测试技能等级为1
                foreach (int bornSkill in unit.Config.BornSkills)
                {
                    unitInfo.Skills[bornSkill] = 1;
                }
            }

            return unitInfo;
        }
        
        // 获取看见unit的玩家，主要用于广播
        public static Dictionary<long, AOIEntity> GetBeSeePlayers(this Unit self)
        {
            return self.GetComponent<AOIEntity>().GetBeSeePlayers();
        }
    }
}