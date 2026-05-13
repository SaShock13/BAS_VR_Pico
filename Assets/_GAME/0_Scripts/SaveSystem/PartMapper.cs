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
            VisualProperties = state.VisualProperties 
        };

        // Transform ????????? ?????? ???? ?????? ?????????
        if (state.LifecycleState == PartLifecycleState.Free && transform != null)
        {
            data.Transform = new TransformSaveData
            {
                Position = transform.position,
                Rotation = transform.rotation
            };
        }

        return data;
    }

    // =========================
    // SAVE to DOMAIN
    // =========================
    public static PartDomainState ToDomain(PartSaveData data)
    {
        if (data == null)
            throw new ArgumentNullException(nameof(data));

        var state = new PartDomainState(data.InstanceId, data.PartId, data.Type);

        // Восстановление визуальных параметров
       
            state.SetVisual(data.VisualProperties);

        // Восстановление логического состояния
        if (data.LifecycleState == PartLifecycleState.Installed)
        {
            if (string.IsNullOrEmpty(data.AttachedSocketId))
                throw new Exception($"SocketId is null for installed part {data.InstanceId}");

            state.AttachToPartSocket(data.AttachedPartId, data.AttachedSocketId);
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

            var socket = parentView.GetSocket(data.AttachedSocketId);

            //var socket = socketResolver.Resolve(data.AttachedSocketId);

            if (socket == null)
                throw new Exception($"Socket not found: {data.AttachedSocketId}");

            view.AttachTo(socket.transform);
        }
        else
        {
            if (data.Transform != null)
            {
                view.transform.position = data.Transform.Position;
                view.transform.rotation = data.Transform.Rotation;
            }

            //view.Detach(); // ???? ???? ??????
        }

        // Применяем визуал
        if (data.VisualProperties is PartVisualProperties visual)
        {
            view.ApplyVisualCommitted(visual);
        }
    }
}