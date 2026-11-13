using System;
using UnityEngine;

public static class PartMapper
{
    // =========================
    // DOMAIN to SAVE
    // =========================
    public static PartSaveData ToSaveData(PartDomainState state, Transform transform)
    {
        if (state == null)
            throw new ArgumentNullException(nameof(state));

        var data = new PartSaveData
        {
            InstanceId = state.InstanceId,
            PartId = state.PartId,
            Type = state.Type,
            LifecycleState = state.LifecycleState,
            AttachedPartId = state.AttachedPartInstanceId,
            AttachedSocketId = state.AttachedSocketId,
            AttachedZoneId = state.IsAttachedToZone ? state.AttachedMountId : null,
            VisualProperties = state.VisualProperties 
        };

        
            data.Transform = new TransformSaveData
            {
                LocalPosition = transform.localPosition,
                LocalRotation = transform.localRotation,
                HasLocalScale = true,
                LocalScale = PartSpawnAnimator.GetStableLocalScale(transform)
            };

        return data;
    }

    // =========================
    // SAVE to DOMAIN
    // =========================
    public static PartDomainState ToDomain(PartSaveData data)
    {
        if (data == null)
            throw new ArgumentNullException(nameof(data));

        var state = new PartDomainState(data.InstanceId, data.PartId, data.Type, data.VisualProperties);

        //// Восстановление визуальных параметров
       
        //    state.SetVisual(data.VisualProperties);

        // Восстановление логического состояния
        if (data.LifecycleState == PartLifecycleState.Installed)
        {
            bool isZone = !string.IsNullOrEmpty(data.AttachedZoneId);
            var mountId = isZone ? data.AttachedZoneId : data.AttachedSocketId;
            if (string.IsNullOrEmpty(mountId))
                throw new Exception($"Mount ID is null for installed part {data.InstanceId}");

            state.AttachToMount(data.AttachedPartId, mountId, isZone);
        }
        else
        {
            state.Detach();
        }

        return state;
    }

    // =========================
    // APPLY VIEW -- Применяет трансформ, 
    // =========================
    public static void ApplyToView(PartSaveData data, DronePartView view, PartViewRegistry viewRegistry)   /// todo Реализовать сокетирование и применение визуала
    {
        if (data == null) throw new ArgumentNullException(nameof(data));
        if (view == null) throw new ArgumentNullException(nameof(view));

        if (data.LifecycleState == PartLifecycleState.Installed)
        {
            viewRegistry.TryGet(data.AttachedPartId, out var parentView);

            bool isZone = !string.IsNullOrEmpty(data.AttachedZoneId);
            MountPoint mount = isZone
                ? parentView.GetMountingZone(data.AttachedZoneId)
                : parentView.GetSocket(data.AttachedSocketId);
            if (mount == null)
                throw new Exception($"Mount not found: {data.AttachedZoneId ?? data.AttachedSocketId}");

            view.AttachTo(mount.transform);
            view.transform.localPosition = data.Transform.LocalPosition;
            view.transform.localRotation = data.Transform.LocalRotation;
        }
        else
        {
            if (data.Transform != null)
            {
                view.transform.position = data.Transform.LocalPosition;
                view.transform.rotation = data.Transform.LocalRotation;
            }

            //view.Detach(); // ???? ???? ??????
        }

        ApplySavedScale(data.Transform, view.transform);

        // Применяем визуал
        if (data.VisualProperties is PartVisualProperties visual)
        {
            view.ApplyVisualCommitted(visual);
        }
    }

    public static void ApplySavedScale(TransformSaveData data, Transform target)
    {
        if (data != null && data.HasLocalScale) target.localScale = data.LocalScale;
    }
}
