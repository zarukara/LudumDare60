using System.Collections.Generic;
using UnityEngine;

namespace ScareSystem
{
    /// <summary>Frees the meshes built in code for this object's children when it goes (they are not assets).</summary>
    public class OwnedMeshes : MonoBehaviour
    {
        public readonly List<Mesh> meshes = new List<Mesh>();

        void OnDestroy()
        {
            foreach (var mesh in meshes)
            {
                if (mesh == null) continue;
                if (Application.isPlaying) Destroy(mesh);
                else DestroyImmediate(mesh);
            }
        }
    }
}
