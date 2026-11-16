using System;
using System.Collections.Generic;
using cfg;
using DotRecast.Core.Numerics;
using Framework;
using GameEntry;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace GameMain
{
    /// <summary>
    /// 客户端单机寻路服务：优先从 AssetBundles/MapData/&lt;场景名&gt;/navmesh.bytes 加载，
    /// 找不到时回退到 Resources/NavData/navmesh.bytes。基于地图数据注册建筑关卡，
    /// 提供与服务器一致的 DotRecast 寻路和关卡通行过滤。
    /// </summary>
    public sealed class LocalPathfindingService
    {
        public const ulong LocalPlayerId = 1;
        private const string LegacyNavMeshAssetPath = "NavData/navmesh";

        private AsyncOperationHandle<TextAsset> _navMeshHandle;
        private bool _hasNavMeshHandle;

        public NavMeshWorld World { get; private set; }
        public Vector3 SpawnPoint { get; private set; }
        public GateAccessStore GateAccess { get; private set; }
        public GateAccessMask CurrentMask { get; private set; }
        public bool GatesReady => GateAccess != null;
        public bool IsReady => World != null;

        public static LocalPathfindingService Create(string mapName)
        {
            var navMeshAsset = LoadNavMeshAsset(mapName, out var handle, out bool fromAddressables);
            if (navMeshAsset == null)
            {
                Debug.LogError("[Recast] 找不到导航数据: " +
                               $"Assets/AssetBundles/MapData/{mapName}/navmesh.bytes");
                return null;
            }

            try
            {
                var service = new LocalPathfindingService
                {
                    World = NavMeshWorld.Load(navMeshAsset.bytes),
                    _navMeshHandle = handle,
                    _hasNavMeshHandle = fromAddressables,
                };
                service.SpawnPoint = new Vector3(
                    service.World.SpawnPoint.X,
                    service.World.SpawnPoint.Y,
                    service.World.SpawnPoint.Z);
                return service;
            }
            catch (Exception ex)
            {
                if (fromAddressables && FW.ResMgr != null)
                    FW.ResMgr.Release(handle);
                Debug.LogError("[Recast] 客户端导航数据加载失败: " + ex);
                return null;
            }
        }

        private static TextAsset LoadNavMeshAsset(string mapName,
            out AsyncOperationHandle<TextAsset> handle, out bool fromAddressables)
        {
            handle = default;
            fromAddressables = false;

            if (FW.ResMgr != null && !string.IsNullOrEmpty(mapName))
            {
                string address = $"Assets/AssetBundles/MapData/{mapName}/navmesh.bytes";
                try
                {
                    var loadHandle = FW.ResMgr.LoadAsset<TextAsset>(address);
                    if (loadHandle.Status == AsyncOperationStatus.Succeeded && loadHandle.Result != null)
                    {
                        handle = loadHandle;
                        fromAddressables = true;
                        return loadHandle.Result;
                    }

                    FW.ResMgr.Release(loadHandle);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("[Recast] Addressables 加载导航数据失败: " + ex.Message);
                }
            }

            // 兼容旧导出路径。
            return Resources.Load<TextAsset>(LegacyNavMeshAssetPath);
        }

        public void Dispose()
        {
            if (_hasNavMeshHandle && FW.ResMgr != null)
                FW.ResMgr.Release(_navMeshHandle);

            _hasNavMeshHandle = false;
            _navMeshHandle = default;
            World = null;
        }

        /// <summary>
        /// 用地图编辑器导出的 BuildingData 注册建筑关卡。
        /// 由 MapDataMgr 在 BuildingData 加载完成后调用。
        /// </summary>
        public void SetBuildingData(BuildingData data)
        {
            if (World == null || data == null)
                return;

            var buildingTable = FW.CfgMgr?.GetTable<TbMapBuildingCfg>();
            var assetTable = FW.CfgMgr?.GetTable<TbAssetPathCfg>();
            if (buildingTable == null)
            {
                Debug.LogWarning("[Recast] 找不到 MapBuildingCfg，无法注册建筑关卡");
                return;
            }

            var definitions = new List<MapGateDefinition>();
            for (int i = 0; i < data.Items.Count; i++)
            {
                var item = data.Items[i];
                if (string.IsNullOrEmpty(item.InstanceId))
                {
                    Debug.LogWarning("[Recast] 建筑关卡缺少 InstanceId，跳过: ConfigId=" + item.Id);
                    continue;
                }

                var config = buildingTable.Get(item.Id);
                if (config == null || !MapBuildingTypeHelper.IsGate(config, assetTable))
                    continue;

                definitions.Add(new MapGateDefinition
                {
                    InstanceId = item.InstanceId,
                    ConfigId = item.Id,
                    PosX = item.Pos.x,
                    PosZ = item.Pos.y,
                    SizeX = item.Size.x,
                    SizeZ = item.Size.y,
                    RotationY = item.RotationY,
                });
            }

            GateAccess = new GateAccessStore(definitions);
            World.RegisterMapGates(definitions);
            RefreshMask();

            Debug.Log($"[Recast] 客户端建筑关卡注册完成，共 {definitions.Count} 个");
        }

        public bool FindPath(Vector3 startFeet, Vector3 target, out List<Vector3> corners, out string error)
        {
            corners = new List<Vector3>();
            if (World == null)
            {
                error = "导航数据未加载";
                return false;
            }

            var rcStart = new RcVec3f(startFeet.x, startFeet.y, startFeet.z);
            var rcTarget = new RcVec3f(target.x, target.y, target.z);

            if (!World.FindPath(rcStart, rcTarget, CurrentMask, out var rcCorners, out error))
                return false;

            for (int i = 0; i < rcCorners.Count; i++)
            {
                var c = rcCorners[i];
                corners.Add(new Vector3(c.X, c.Y, c.Z));
            }

            return true;
        }

        public void RefreshMask()
        {
            CurrentMask = GateAccess != null ? GateAccess.BuildMask(LocalPlayerId) : null;
        }

        public GateInfoSnapshot GetGateInfo(string instanceId)
        {
            return GateAccess?.GetInfo(instanceId, LocalPlayerId, true);
        }

        public GateOperationResult ApplyGate(string instanceId)
        {
            var result = GateAccess?.Apply(instanceId, LocalPlayerId);
            RefreshMask();
            return result;
        }

        public GateOperationResult ApproveGate(string instanceId, ulong playerId)
        {
            var result = GateAccess?.Approve(instanceId, LocalPlayerId, playerId);
            RefreshMask();
            return result;
        }

        public GateOperationResult RemoveGateAccess(string instanceId, ulong playerId)
        {
            var result = GateAccess?.RemoveAccess(instanceId, LocalPlayerId, playerId);
            RefreshMask();
            return result;
        }

        public GateOperationResult ClaimGate(string instanceId)
        {
            var result = GateAccess?.Claim(instanceId, LocalPlayerId);
            RefreshMask();
            return result;
        }

        public bool TrySetGlobalGateState(int gateId, bool open)
        {
            return World != null && World.TrySetGateState(gateId, open);
        }
    }
}
