﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Unity.Mathematics;

namespace ET.Server
{
	[ActorMessageHandler(SceneType.Map)]
	public class M2M_UnitTransferRequestHandler : AMActorRpcHandler<Scene, M2M_UnitTransferRequest, M2M_UnitTransferResponse>
	{
		protected override async ETTask Run(Scene scene, M2M_UnitTransferRequest request, M2M_UnitTransferResponse response)
		{
			UnitComponent unitComponent = scene.GetComponent<UnitComponent>();
			Unit unit = MongoHelper.Deserialize<Unit>(request.Unit);
			
			unitComponent.AddChild(unit);
			// unitComponent.Add(unit);

			foreach (byte[] bytes in request.Entitys)
			{
				Entity entity = MongoHelper.Deserialize<Entity>(bytes);
				unit.AddComponent(entity);
			}
			
			unit.AddComponent<MoveComponent>();
            
            //暂不需要巡逻组件
			// unit.AddComponent<PathfindingComponent, string>(scene.Name);
			unit.Position = new float3(0, 0, 0);
			
			unit.AddComponent<MailBoxComponent>();

			// 通知客户端开始切场景
			M2C_StartSceneChange m2CStartSceneChange = new M2C_StartSceneChange() {SceneInstanceId = scene.InstanceId, SceneName = scene.Name};
			MessageHelper.SendToClient(unit, m2CStartSceneChange);
			
			// 通知客户端创建My Unit
			M2C_CreateMyUnit m2CCreateUnits = new M2C_CreateMyUnit();
			m2CCreateUnits.Unit = UnitHelper.CreateUnitInfo(unit);
			MessageHelper.SendToClient(unit, m2CCreateUnits);
			
			// 加入aoi
			unit.AddComponent<AOIEntity, int, float3>(9 * 1000, unit.Position);
            unit.AddComponent<BattleUnitComponent, List<int>>(unit.Config.BornSkills.ToList());
            float radius = unit.GetComponent<NumericComponent>().GetAsInt(NumericType.Hp) / 100f;
            unit.AddComponent<CollisionComponent>().AddCollider(EColliderType.Circle, Vector2.One * radius, Vector2.Zero, true, unit);

            // 解锁location，可以接收发给Unit的消息
            await LocationProxyComponent.Instance.UnLock(LocationType.Unit, unit.Id, request.OldInstanceId, unit.InstanceId);
		}
	}
}