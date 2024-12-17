using FantasyVoxels.AssetManagement;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FantasyVoxels.Entities.EntityModels
{
    public class EntityModel
    {
        string mpath, tpath;
        float scale;
        Model model;
        Texture2D texture;
        Dictionary<string, Matrix> partMatrixLookup = new Dictionary<string, Matrix>();
        public EntityModel(string path, string tpath, float scale = 1f)
        {
            this.mpath = path;
            this.tpath = tpath;

            model = AssetServer.RequestOrLoad<Model>(mpath);
            texture = AssetServer.RequestOrLoad<Texture2D>(tpath);
            this.scale = scale;

            foreach (var mesh in model.Meshes)
            {
                partMatrixLookup.Add(mesh.Name,Matrix.Identity);
            }
        }
        public Matrix GetPartMatrix(string name)
        {
            return partMatrixLookup.TryGetValue(name, out var modelMesh) ? modelMesh : Matrix.Identity;
        }
        public void SetPartMatrix(string name, Matrix val)
        {
            partMatrixLookup[name] = val;
        }
        public void Render(Matrix entityMatrix)
        {
            MGame.Instance.GrabVoxelData((Vector3)entityMatrix.Translation, out var voxelData);

            float ourLight = (voxelData.skyLight / 255f) * MGame.Instance.daylightPercentage;

            foreach (var mesh in model.Meshes)
            {
                var partmat = partMatrixLookup[mesh.Name];
                foreach (var part in mesh.MeshParts)
                {
                    part.Effect = MGame.Instance.GetEntityShader();
                    part.Effect.Parameters["mainTexture"].SetValue(texture);
                    Matrix world = partmat * mesh.ParentBone.ModelTransform * Matrix.CreateScale(scale) * entityMatrix * MGame.Instance.world;
                    part.Effect.Parameters["World"].SetValue(world);
                    world.Translation = Vector3.Zero;
                    part.Effect.Parameters["RotWorld"].SetValue(world);
                    part.Effect.Parameters["View"].SetValue(MGame.Instance.view);
                    part.Effect.Parameters["Projection"].SetValue(MGame.Instance.projection);

                    part.Effect.Parameters["tint"].SetValue(ourLight);
                    part.Effect.Parameters["blocklightTint"].SetValue(voxelData.blockLight / 255f);
                }
                mesh.Draw();
            }
        }
    }
}
