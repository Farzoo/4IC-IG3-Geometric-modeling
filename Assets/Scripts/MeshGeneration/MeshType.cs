using UnityEngine;

namespace TP1
{
    public enum MeshType
    {
        Box,
        Chips,
        Pyramid,
    }

    public static class MeshTypeFactory
    {
        public static IMeshMaker CreateMeshMaker(this MeshType meshType, Vector3 halfSize)
        {
            return meshType switch
            {
                MeshType.Box => new BoxMaker(halfSize),
                MeshType.Chips => new Chips(halfSize),
                MeshType.Pyramid => new PyramidMaker(),
                _ => throw new System.NotSupportedException($"MeshType {meshType} is not supported.")
            };
        }
    }
}