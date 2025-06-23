using UnityEngine;

namespace TP1
{
    public enum MeshType
    {
        Box,
        Chips,
    }

    public static class MeshTypeFactory
    {
        public static IMeshMaker CreateMeshMaker(this MeshType meshType, Vector3 halfSize)
        {
            return meshType switch
            {
                MeshType.Box => new BoxMaker(halfSize),
                MeshType.Chips => new Chips(halfSize),
                _ => throw new System.NotSupportedException($"MeshType {meshType} is not supported.")
            };
        }
    }
}