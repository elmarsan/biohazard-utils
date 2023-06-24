﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using emdui.Extensions;
using IntelOrca.Biohazard;

namespace emdui
{
    public class ModelScene
    {
        private BitmapSource _texture;
        private Model3DGroup _root;
        private Model3DGroup[] _armature;
        private GeometryModel3D[] _model3d;
        private GeometryModel3D _highlightedModel;

        private IModelMesh _mesh;
        private Emr _emr;
        private int _numParts;

        public void SetKeyframe(int keyframeIndex)
        {
            for (var i = 0; i < _armature.Length; i++)
            {
                if (_armature[i] is Model3DGroup armature)
                {
                    armature.Transform = GetTransformation(i, keyframeIndex);
                }
            }
        }

        public ModelVisual3D CreateVisual3d()
        {
            return new ModelVisual3D()
            {
                Content = _root
            };
        }

        public GeometryModel3D GetModel3d(int partIndex)
        {
            return partIndex < 0 || partIndex >= _model3d.Length ? null : _model3d[partIndex];
        }

        public void HighlightPart(int partIndex)
        {
            if (_highlightedModel != null)
            {
                _highlightedModel.Material = CreateMaterial(false);
                _highlightedModel.BackMaterial = _highlightedModel.Material;
                _highlightedModel = null;
            }

            var model3d = GetModel3d(partIndex);
            if (model3d == null)
                return;

            model3d.Material = CreateMaterial(true);
            model3d.BackMaterial = model3d.Material;
            _highlightedModel = model3d;
        }

        public void GenerateFrom(IModelMesh mesh, Emr emr, TimFile timFile)
        {
            _emr = emr;
            _texture = timFile.ToBitmap();
            _mesh = mesh;
            _numParts = _mesh.NumParts;
            _model3d = new GeometryModel3D[_numParts];
            _armature = new Model3DGroup[_numParts];
            _root = CreateModel();
        }

        private Model3DGroup CreateModel()
        {
            var rootGroup = new Model3DGroup();

            var armatureParts = new int[0];

            if (_emr != null && _emr.NumParts != 0 && Settings.Default.ShowFloor)
            {
                rootGroup.Children.Add(CreateFloor());
            }
            if (_emr != null && _emr.NumParts != 0)
            {
                var main = CreateModelFromArmature(0);
                if (main != null)
                {
                    rootGroup.Children.Add(main);
                    armatureParts = GetAllArmatureParts(0);
                }
            }
            for (var i = 0; i < _numParts; i++)
            {
                if (!armatureParts.Contains(i))
                {
                    var model = CreateModelFromPart(i);
                    rootGroup.Children.Add(model);
                }
            }
            return rootGroup;
        }

        private GeometryModel3D CreateFloor()
        {
            var size = 5000;

            var mesh = CreateCubeMesh(size, 256, size);
            var material = new DiffuseMaterial(Brushes.Gray);
            var floor = new GeometryModel3D()
            {
                Geometry = mesh,
                Material = material,
                Transform = new TranslateTransform3D(0, 128, 0)
            };
            return floor;
        }

        private static MeshGeometry3D CreateCubeMesh(int width, int height, int depth)
        {

            // Vertex positions
            var positions = new Point3D[]
            {
                new Point3D(-width / 2, -height / 2, -depth / 2), // 0
                new Point3D(-width / 2, +height / 2, -depth / 2), // 1
                new Point3D(+width / 2, +height / 2, -depth / 2), // 2
                new Point3D(+width / 2, -height / 2, -depth / 2), // 3
                new Point3D(-width / 2, -height / 2, +depth / 2), // 4
                new Point3D(-width / 2, +height / 2, +depth / 2), // 5
                new Point3D(+width / 2, +height / 2, +depth / 2), // 6
                new Point3D(+width / 2, -height / 2, +depth / 2)  // 7
            };

            // Triangle indices
            var triangleIndices = new int[]
            {
                // Bottom face
                0, 1, 2,
                0, 2, 3,

                // Top face
                5, 4, 7,
                5, 7, 6,

                // Front face
                1, 5, 6,
                1, 6, 2,

                // Back face
                4, 0, 3,
                4, 3, 7,

                // Left face
                4, 5, 1,
                4, 1, 0,

                // Right face
                3, 2, 6,
                3, 6, 7
            };

            // Normals
            var normals = new[]
            {
                new Vector3D(0, 0, -1), // Bottom face
                new Vector3D(0, 0, 1),  // Top face
                new Vector3D(0, 1, 0),  // Front face
                new Vector3D(0, -1, 0), // Back face
                new Vector3D(-1, 0, 0), // Left face
                new Vector3D(1, 0, 0)   // Right face
            };

            var mesh = new MeshGeometry3D();
            mesh.Positions = new Point3DCollection(positions);
            mesh.Normals = new Vector3DCollection(normals);
            mesh.TriangleIndices = new Int32Collection(triangleIndices);
            return mesh;
        }

        private int[] GetAllArmatureParts(int rootPartIndex)
        {
            var emr = _emr;
            var parts = new List<int>();
            var stack = new Stack<byte>();
            stack.Push((byte)rootPartIndex);
            while (stack.Count != 0)
            {
                var partIndex = stack.Pop();
                parts.Add(partIndex);

                var children = emr.GetArmatureParts(partIndex);
                foreach (var child in children)
                {
                    stack.Push(child);
                }
            }
            return parts.ToArray();
        }

        private Model3DGroup CreateModelFromArmature(int partIndex)
        {
            if (_armature.Length <= partIndex)
                return null;

            var armature = new Model3DGroup();
            var armatureMesh = CreateModelFromPart(partIndex);
            if (armatureMesh != null)
                armature.Children.Add(armatureMesh);

            // Children
            var subParts = _emr.GetArmatureParts(partIndex);
            foreach (var subPart in subParts)
            {
                var subPartMesh = CreateModelFromArmature(subPart);
                if (subPartMesh != null)
                {
                    armature.Children.Add(subPartMesh);
                }
            }

            armature.Transform = GetTransformation(partIndex, -1);
            _armature[partIndex] = armature;
            return armature;
        }

        private Transform3D GetTransformation(int partIndex, int keyFrameIndex)
        {
            var emr = _emr;
            var relativePosition = emr.GetRelativePosition(partIndex);

            var transformGroup = new Transform3DGroup();
            if (keyFrameIndex != -1)
            {
                var keyFrame = emr.KeyFrames[keyFrameIndex];
                if (partIndex == 0)
                {
                    relativePosition = keyFrame.Offset;
                }

                var angle = keyFrame.GetAngle(partIndex);
                var rx = (angle.x / 4096.0) * 360;
                var ry = (angle.y / 4096.0) * 360;
                var rz = (angle.z / 4096.0) * 360;

                transformGroup.Children.Add(new RotateTransform3D(
                    new AxisAngleRotation3D(new Vector3D(0, 0, 1), rz)));
                transformGroup.Children.Add(new RotateTransform3D(
                    new AxisAngleRotation3D(new Vector3D(0, 1, 0), ry)));
                transformGroup.Children.Add(new RotateTransform3D(
                    new AxisAngleRotation3D(new Vector3D(1, 0, 0), rx)));
            }

            transformGroup.Children.Add(new TranslateTransform3D(relativePosition.x, relativePosition.y, relativePosition.z));
            return transformGroup;
        }

        private GeometryModel3D CreateModelFromPart(int partIndex)
        {
            var textureSize = new Size(_texture.PixelWidth, _texture.PixelHeight);
            var model = new GeometryModel3D();
            if (_mesh.NumParts <= partIndex)
                return null;

            model.Geometry = CreateMesh(_mesh, partIndex, textureSize);
            model.Material = CreateMaterial(false);
            model.BackMaterial = model.Material;

            _model3d[partIndex] = model;
            return model;
        }

        private Material CreateMaterial(bool highlighted)
        {
            var material = new DiffuseMaterial();
            material.Brush = new ImageBrush(_texture)
            {
                TileMode = TileMode.Tile,
                ViewportUnits = BrushMappingMode.Absolute
            };
            if (highlighted)
                material.AmbientColor = Colors.Blue;
            return material;
        }

        private static MeshGeometry3D CreateMesh(IModelMesh mesh, int partIndex, Size textureSize)
        {
            var visitor = new MeshGeometry3DMeshVisitor(partIndex, textureSize);
            visitor.Accept(mesh);
            return visitor.Mesh;
        }

        private static MeshGeometry3D CreateMesh(Md1 md1, int partIndex, Size textureSize)
        {
            var textureWidth = (double)textureSize.Width;
            var textureHeight = (double)textureSize.Height;
            var mesh = new MeshGeometry3D();

            // Triangles
            {
                var objTriangles = md1.Objects[partIndex * 2];
                var dataTriangles = md1.GetTriangles(objTriangles);
                var dataTriangleTextures = md1.GetTriangleTextures(objTriangles);
                var dataPositions = md1.GetPositionData(objTriangles);
                var dataNormals = md1.GetNormalData(objTriangles);
                for (var i = 0; i < dataTriangles.Length; i++)
                {
                    var triangle = dataTriangles[i];
                    var texture = dataTriangleTextures[i];

                    mesh.Positions.Add(dataPositions[triangle.v0].ToPoint3D());
                    mesh.Positions.Add(dataPositions[triangle.v1].ToPoint3D());
                    mesh.Positions.Add(dataPositions[triangle.v2].ToPoint3D());

                    mesh.Normals.Add(dataNormals[triangle.n0].ToVector3D());
                    mesh.Normals.Add(dataNormals[triangle.n1].ToVector3D());
                    mesh.Normals.Add(dataNormals[triangle.n2].ToVector3D());

                    var page = texture.page & 0x0F;
                    var offsetU = page * 128;
                    mesh.TextureCoordinates.Add(new Point((offsetU + texture.u0) / textureWidth, (texture.v0 / textureHeight)));
                    mesh.TextureCoordinates.Add(new Point((offsetU + texture.u1) / textureWidth, (texture.v1 / textureHeight)));
                    mesh.TextureCoordinates.Add(new Point((offsetU + texture.u2) / textureWidth, (texture.v2 / textureHeight)));
                }
            }

            // Quads
            {
                var objQuads = md1.Objects[(partIndex * 2) + 1];
                var dataQuads = md1.GetQuads(objQuads);
                var dataPositions = md1.GetPositionData(objQuads);
                var dataNormals = md1.GetNormalData(objQuads);
                var dataQuadTextures = md1.GetQuadTextures(objQuads);
                for (var i = 0; i < dataQuads.Length; i++)
                {
                    var quad = dataQuads[i];
                    var texture = dataQuadTextures[i];
                    mesh.Positions.Add(dataPositions[quad.v0].ToPoint3D());
                    mesh.Positions.Add(dataPositions[quad.v1].ToPoint3D());
                    mesh.Positions.Add(dataPositions[quad.v2].ToPoint3D());

                    mesh.Normals.Add(dataNormals[quad.n0].ToVector3D());
                    mesh.Normals.Add(dataNormals[quad.n1].ToVector3D());
                    mesh.Normals.Add(dataNormals[quad.n2].ToVector3D());

                    var page = texture.page & 0x0F;
                    var offsetU = page * 128;
                    mesh.TextureCoordinates.Add(new Point((offsetU + texture.u0) / textureWidth, (texture.v0 / textureHeight)));
                    mesh.TextureCoordinates.Add(new Point((offsetU + texture.u1) / textureWidth, (texture.v1 / textureHeight)));
                    mesh.TextureCoordinates.Add(new Point((offsetU + texture.u2) / textureWidth, (texture.v2 / textureHeight)));

                    mesh.Positions.Add(dataPositions[quad.v3].ToPoint3D());
                    mesh.Positions.Add(dataPositions[quad.v2].ToPoint3D());
                    mesh.Positions.Add(dataPositions[quad.v1].ToPoint3D());
                    mesh.Normals.Add(dataNormals[quad.n3].ToVector3D());
                    mesh.Normals.Add(dataNormals[quad.n2].ToVector3D());
                    mesh.Normals.Add(dataNormals[quad.n1].ToVector3D());
                    mesh.TextureCoordinates.Add(new Point((offsetU + texture.u3) / textureWidth, (texture.v3 / textureHeight)));
                    mesh.TextureCoordinates.Add(new Point((offsetU + texture.u2) / textureWidth, (texture.v2 / textureHeight)));
                    mesh.TextureCoordinates.Add(new Point((offsetU + texture.u1) / textureWidth, (texture.v1 / textureHeight)));
                }
            }

            return mesh;
        }

        private static MeshGeometry3D CreateMesh(Md2 md2, int partIndex, Size textureSize)
        {
            var textureWidth = (double)textureSize.Width;
            var textureHeight = (double)textureSize.Height;
            var mesh = new MeshGeometry3D();

            var obj = md2.Objects[partIndex];
            var dataPositions = md2.GetPositionData(obj);
            var dataNormals = md2.GetNormalData(obj);

            // Triangles
            var dataTriangles = md2.GetTriangles(obj);
            for (var i = 0; i < dataTriangles.Length; i++)
            {
                var triangle = dataTriangles[i];

                mesh.Positions.Add(dataPositions[triangle.v0].ToPoint3D());
                mesh.Positions.Add(dataPositions[triangle.v1].ToPoint3D());
                mesh.Positions.Add(dataPositions[triangle.v2].ToPoint3D());

                mesh.Normals.Add(dataNormals[triangle.v0].ToVector3D());
                mesh.Normals.Add(dataNormals[triangle.v1].ToVector3D());
                mesh.Normals.Add(dataNormals[triangle.v2].ToVector3D());

                var page = triangle.page & 0x0F;
                var offsetU = page * 128;
                mesh.TextureCoordinates.Add(new Point((offsetU + triangle.tu0) / textureWidth, (triangle.tv0 / textureHeight)));
                mesh.TextureCoordinates.Add(new Point((offsetU + triangle.tu1) / textureWidth, (triangle.tv1 / textureHeight)));
                mesh.TextureCoordinates.Add(new Point((offsetU + triangle.tu2) / textureWidth, (triangle.tv2 / textureHeight)));
            }

            // Quads
            var dataQuads = md2.GetQuads(obj);
            for (var i = 0; i < dataQuads.Length; i++)
            {
                var quad = dataQuads[i];
                mesh.Positions.Add(dataPositions[quad.v0].ToPoint3D());
                mesh.Positions.Add(dataPositions[quad.v1].ToPoint3D());
                mesh.Positions.Add(dataPositions[quad.v2].ToPoint3D());

                mesh.Normals.Add(dataNormals[quad.v0].ToVector3D());
                mesh.Normals.Add(dataNormals[quad.v1].ToVector3D());
                mesh.Normals.Add(dataNormals[quad.v2].ToVector3D());

                var page = quad.page & 0x0F;
                var offsetU = page * 128;
                mesh.TextureCoordinates.Add(new Point((offsetU + quad.tu0) / textureWidth, (quad.tv0 / textureHeight)));
                mesh.TextureCoordinates.Add(new Point((offsetU + quad.tu1) / textureWidth, (quad.tv1 / textureHeight)));
                mesh.TextureCoordinates.Add(new Point((offsetU + quad.tu2) / textureWidth, (quad.tv2 / textureHeight)));

                mesh.Positions.Add(dataPositions[quad.v3].ToPoint3D());
                mesh.Positions.Add(dataPositions[quad.v2].ToPoint3D());
                mesh.Positions.Add(dataPositions[quad.v1].ToPoint3D());
                mesh.Normals.Add(dataNormals[quad.v3].ToVector3D());
                mesh.Normals.Add(dataNormals[quad.v2].ToVector3D());
                mesh.Normals.Add(dataNormals[quad.v1].ToVector3D());
                mesh.TextureCoordinates.Add(new Point((offsetU + quad.tu3) / textureWidth, (quad.tv3 / textureHeight)));
                mesh.TextureCoordinates.Add(new Point((offsetU + quad.tu2) / textureWidth, (quad.tv2 / textureHeight)));
                mesh.TextureCoordinates.Add(new Point((offsetU + quad.tu1) / textureWidth, (quad.tv1 / textureHeight)));
            }

            return mesh;
        }

        private class MeshGeometry3DMeshVisitor : MeshVisitor
        {
            private readonly int _partIndex;
            private readonly Size _textureSize;
            private readonly List<Vector> _positions = new List<Vector>();
            private readonly List<Vector> _normals = new List<Vector>();
            private byte _page;

            public MeshGeometry3D Mesh { get; } = new MeshGeometry3D();

            public MeshGeometry3DMeshVisitor(int partIndex, Size textureSize)
            {
                _partIndex = partIndex;
                _textureSize = textureSize;
                Trianglulate = true;
            }

            public override bool VisitPart(int index)
            {
                _positions.Clear();
                _normals.Clear();
                return index == _partIndex;
            }

            public override void VisitPrimitive(int numPoints, byte page)
            {
                _page = page;
            }

            public override void VisitPosition(Vector value)
            {
                _positions.Add(value);
            }

            public override void VisitNormal(Vector value)
            {
                _normals.Add(value);
            }

            public override void VisitPrimitivePoint(ushort v, ushort n, byte tu, byte tv)
            {
                Mesh.Positions.Add(_positions[v].ToPoint3D());
                Mesh.Normals.Add(_normals[n].ToVector3D());

                var offsetTu = _page * 128;
                Mesh.TextureCoordinates.Add(new Point((offsetTu + tu) / _textureSize.Width, tv / _textureSize.Height));
            }
        }
    }
}
