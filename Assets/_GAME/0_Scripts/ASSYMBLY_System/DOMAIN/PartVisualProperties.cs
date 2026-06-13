using System;
using UnityEngine;

[Serializable]
public struct PartVisualProperties
{
    public Color Color;

    public string MaterialAddress; // todo убрать, когда перейдем на MaterialId и MaterialDefinition

    public string MaterialId;

    [Range(0, 1)]
    public float Smoothness;

    [Range(0, 1)]
    public float Metallic ;
}