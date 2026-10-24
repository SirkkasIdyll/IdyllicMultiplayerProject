using System;
using ENet;
using Game.Temperance.Network;
using Godot;
using Vector2 = Godot.Vector2;

namespace Game.Testament;

using GdUnit4;
using static GdUnit4.Assertions;

[TestSuite][RequireGodotRuntime]
public class BufferTest
{
    [TestCase]
    public void BoolReadAndWriteTest()
    {
        var buffer = new byte[5];
        var bufferWriter = new BufferWriter(buffer);
        // 1111 0011
        bufferWriter.WriteBool(true);
        bufferWriter.WriteBool(true);
        bufferWriter.WriteBool(false);
        bufferWriter.WriteBool(false);
        bufferWriter.WriteBool(true);
        bufferWriter.WriteBool(true);
        bufferWriter.WriteBool(true);
        bufferWriter.WriteBool(true);
        
        // 0101 0000
        bufferWriter.WriteBool(false);
        bufferWriter.WriteBool(false);
        bufferWriter.WriteBool(false);
        bufferWriter.WriteBool(false);
        bufferWriter.WriteBool(true);
        bufferWriter.WriteBool(false);
        bufferWriter.WriteBool(true);
        bufferWriter.WriteBool(false);
        
        // 0010 0010
        bufferWriter.WriteBool(false);
        bufferWriter.WriteBool(true);
        bufferWriter.WriteBool(false);
        bufferWriter.WriteBool(false);
        bufferWriter.WriteBool(false);
        bufferWriter.WriteBool(true);
        bufferWriter.WriteBool(false);
        bufferWriter.WriteBool(false);
        
        // 0101 0000
        bufferWriter.WriteBool(false);
        bufferWriter.WriteBool(false);
        bufferWriter.WriteBool(false);
        bufferWriter.WriteBool(false);
        bufferWriter.WriteBool(true);
        bufferWriter.WriteBool(false);
        bufferWriter.WriteBool(true);
        bufferWriter.WriteBool(false);
        
        // 0000 0101
        bufferWriter.WriteBool(true);
        bufferWriter.WriteBool(false);
        bufferWriter.WriteBool(true);
        bufferWriter.WriteBool(false);
        bufferWriter.WriteBool(false);
        bufferWriter.WriteBool(false);
        bufferWriter.WriteBool(false);
        bufferWriter.WriteBool(false);

        bufferWriter.FinalWrite(out _);

        var bufferReader = new BufferReader(buffer);
        AssertBool(bufferReader.ReadBool()).IsTrue();
        AssertBool(bufferReader.ReadBool()).IsTrue();
        AssertBool(bufferReader.ReadBool()).IsFalse();
        AssertBool(bufferReader.ReadBool()).IsFalse();
        AssertBool(bufferReader.ReadBool()).IsTrue();
        AssertBool(bufferReader.ReadBool()).IsTrue();
        AssertBool(bufferReader.ReadBool()).IsTrue();
        AssertBool(bufferReader.ReadBool()).IsTrue();
        
        AssertBool(bufferReader.ReadBool()).IsFalse();
        AssertBool(bufferReader.ReadBool()).IsFalse();
        AssertBool(bufferReader.ReadBool()).IsFalse();
        AssertBool(bufferReader.ReadBool()).IsFalse();
        AssertBool(bufferReader.ReadBool()).IsTrue();
        AssertBool(bufferReader.ReadBool()).IsFalse();
        AssertBool(bufferReader.ReadBool()).IsTrue();
        AssertBool(bufferReader.ReadBool()).IsFalse();
        
        AssertBool(bufferReader.ReadBool()).IsFalse();
        AssertBool(bufferReader.ReadBool()).IsTrue();
        AssertBool(bufferReader.ReadBool()).IsFalse();
        AssertBool(bufferReader.ReadBool()).IsFalse();
        AssertBool(bufferReader.ReadBool()).IsFalse();
        AssertBool(bufferReader.ReadBool()).IsTrue();
        AssertBool(bufferReader.ReadBool()).IsFalse();
        AssertBool(bufferReader.ReadBool()).IsFalse();
        
        AssertBool(bufferReader.ReadBool()).IsFalse();
        AssertBool(bufferReader.ReadBool()).IsFalse();
        AssertBool(bufferReader.ReadBool()).IsFalse();
        AssertBool(bufferReader.ReadBool()).IsFalse();
        AssertBool(bufferReader.ReadBool()).IsTrue();
        AssertBool(bufferReader.ReadBool()).IsFalse();
        AssertBool(bufferReader.ReadBool()).IsTrue();
        AssertBool(bufferReader.ReadBool()).IsFalse();
        
        AssertBool(bufferReader.ReadBool()).IsTrue();
        AssertBool(bufferReader.ReadBool()).IsFalse();
        AssertBool(bufferReader.ReadBool()).IsTrue();
        AssertBool(bufferReader.ReadBool()).IsFalse();
        AssertBool(bufferReader.ReadBool()).IsFalse();
        AssertBool(bufferReader.ReadBool()).IsFalse();
        AssertBool(bufferReader.ReadBool()).IsFalse();
        AssertBool(bufferReader.ReadBool()).IsFalse();
    }

    [TestCase]
    [ThrowsException(typeof(InvalidOperationException))]
    public void BufferOverflowTest()
    {
        var buffer = new byte[4];
        var bufferWriter = new BufferWriter(buffer);
        bufferWriter.WriteInt(int.MaxValue);
        bufferWriter.WriteInt(int.MaxValue);
    }

    [TestCase]
    [ThrowsException(typeof(InvalidOperationException))]
    public void BufferUnderflowTest()
    {
        var buffer = new byte[4];
        var bufferWriter = new BufferWriter(buffer);
        bufferWriter.WriteInt(int.MaxValue);
        
        var bufferReader = new BufferReader(buffer);
        AssertInt(bufferReader.ReadInt()).IsEqual(int.MaxValue);
        bufferReader.ReadInt(0, 1);
    }

    [TestCase]
    public void IntReadAndWriteTest()
    {
        var buffer = new byte[5];
        var bufferWriter = new BufferWriter(buffer);
        // Take up an entire 32-bit word by using the max value with the absolute minimum and maximum range
        bufferWriter.WriteInt(int.MaxValue);

        // 1111 1111 = 255 so we whatever the number, we should only need 8 bits for this :)
        var min = 0;
        var max = 255;
        var randomValue = Random.Shared.Next(min, max);
        bufferWriter.WriteInt(randomValue, min, max);
        
        // I forgot to do the FinalWrite so this test errored a few times, I feel like this is going to be a recurring trend
        bufferWriter.FinalWrite(out _); 
        
        var bufferReader = new BufferReader(buffer);
        AssertInt(bufferReader.ReadInt()).IsEqual(int.MaxValue);
        AssertInt(bufferReader.ReadInt(min, max)).IsEqual(randomValue);
    }
    
    [TestCase]
    public void FloatReadAndWriteTest()
    {
        var buffer = new byte[6];
        var bufferWriter = new BufferWriter(buffer);
        // It should take two bytes this time to handle this range at this precision level
        var min = -5.25f;
        var max = 2.68f;
        var precision = 0.01f;
        var range = max - min;
        var randomValue1 = Random.Shared.NextSingle() * range + min;
        var randomValue2 = Random.Shared.NextSingle() * range + min;
        var randomValue3 = Random.Shared.NextSingle() * range + min;
        bufferWriter.WriteFloat(randomValue1, min, max, precision);
        bufferWriter.WriteFloat(randomValue2, min, max, precision);
        bufferWriter.WriteFloat(randomValue3, min, max, precision);
        bufferWriter.FinalWrite(out _);
        
        var bufferReader = new BufferReader(buffer);
        // If we are +- half the precision range, we are all good
        AssertFloat(bufferReader.ReadFloat(min, max, precision)).IsEqualApprox(randomValue1, precision / 2);
        AssertFloat(bufferReader.ReadFloat(min, max, precision)).IsEqualApprox(randomValue2, precision / 2);
        AssertFloat(bufferReader.ReadFloat(min, max, precision)).IsEqualApprox(randomValue3, precision / 2);
    }

    [TestCase]
    public void GodotTypesTest()
    {
        // Surely large enough to test every single type
        var buffer = new byte[1200];
        var bufferWriter = new BufferWriter(buffer);
        var min = Random.Shared.NextSingle() * -10;
        var max = Random.Shared.NextSingle() * 10;
        var range = max - min;
        var precision = 0.01f;
        
        // Vector2
        var vector2 = new Vector2(
            Random.Shared.NextSingle() * range + min,
            Random.Shared.NextSingle() * range + min
        );
        bufferWriter.WriteVector2(vector2, min, max, precision);
        
        // Vector2I
        var vector2I = new Vector2I(
            Random.Shared.Next((int)min, (int)max),
            Random.Shared.Next((int)min, (int)max)
        );
        bufferWriter.WriteVector2I(vector2I, (int)min, (int)max);
        
        // Rect2
        var rect2Position = new Vector2(
            Random.Shared.NextSingle() * range + min,
            Random.Shared.NextSingle() * range + min
        );
        var rect2Size = new Vector2(
            Random.Shared.NextSingle() * range + min,
            Random.Shared.NextSingle() * range + min
        );
        var rect2 = new Rect2(rect2Position, rect2Size);
        bufferWriter.WriteRect2(rect2, min, max, precision);

        // Rect2I
        var rect2IPosition = new Vector2I(
            Random.Shared.Next((int)min, (int)max),
            Random.Shared.Next((int)min, (int)max)
        );
        var rect2ISize = new Vector2I(
            Random.Shared.Next((int)min, (int)max),
            Random.Shared.Next((int)min, (int)max)
        );
        var rect2I = new Rect2I(rect2IPosition, rect2ISize);
        bufferWriter.WriteRect2I(rect2I, (int)min, (int)max);
        
        // Vector3
        var vector3 = new Vector3(
            Random.Shared.NextSingle() * range + min,
            Random.Shared.NextSingle() * range + min,
            Random.Shared.NextSingle() * range + min
            );
        bufferWriter.WriteVector3(vector3, min, max, precision);
        
        // Vector3I
        var vector3I = new Vector3I(
            Random.Shared.Next((int)min, (int)max),
            Random.Shared.Next((int)min, (int)max),
            Random.Shared.Next((int)min, (int)max)
        );
        bufferWriter.WriteVector3I(vector3I, (int)min, (int)max);
        
        // Basis
        var basisX = new Vector3(
            Random.Shared.NextSingle() * range + min,
            Random.Shared.NextSingle() * range + min,
            Random.Shared.NextSingle() * range + min
        );
        var basisY = new Vector3(
            Random.Shared.NextSingle() * range + min,
            Random.Shared.NextSingle() * range + min,
            Random.Shared.NextSingle() * range + min
        );
        var basisZ = new Vector3(
            Random.Shared.NextSingle() * range + min,
            Random.Shared.NextSingle() * range + min,
            Random.Shared.NextSingle() * range + min
        );
        var basis = new Basis(basisX, basisY, basisZ);
        bufferWriter.WriteBasis(basis, min, max, precision);
        
        // Transform3D
        var transform3DOrigin = new Vector3(
            Random.Shared.NextSingle() * range + min,
            Random.Shared.NextSingle() * range + min,
            Random.Shared.NextSingle() * range + min
        );
        var transform3D = new Transform3D(basis, transform3DOrigin);
        bufferWriter.WriteTransform3D(transform3D, min, max, precision);
        
        // Vector4
        var vector4 = new Vector4(
            Random.Shared.NextSingle() * range + min,
            Random.Shared.NextSingle() * range + min,
            Random.Shared.NextSingle() * range + min,
            Random.Shared.NextSingle() * range + min
        );
        bufferWriter.WriteVector4(vector4, min, max, precision);
        
        // Vector4I
        var vector4I = new Vector4I(
            Random.Shared.Next((int)min, (int)max),
            Random.Shared.Next((int)min, (int)max),
            Random.Shared.Next((int)min, (int)max),
            Random.Shared.Next((int)min, (int)max)
        );
        bufferWriter.WriteVector4I(vector4I, (int)min, (int)max);
        
        // Plane
        var d = Random.Shared.NextSingle() * range + min;
        var normal = new Vector3(
            Random.Shared.NextSingle() * range + min,
            Random.Shared.NextSingle() * range + min,
            Random.Shared.NextSingle() * range + min
        );
        var plane = new Plane(normal, d);
        bufferWriter.WritePlane(plane, min, max, precision);
        
        // Aabb
        var aabbPosition = new Vector3(
            Random.Shared.NextSingle() * range + min,
            Random.Shared.NextSingle() * range + min,
            Random.Shared.NextSingle() * range + min
        );
        var aabbSize = new Vector3(
            Random.Shared.NextSingle() * range + min,
            Random.Shared.NextSingle() * range + min,
            Random.Shared.NextSingle() * range + min
        );
        var aabb = new Aabb(aabbPosition, aabbSize);
        bufferWriter.WriteAabb(aabb, min, max, precision);
        
        // Quaternion
        var quaternion = new Quaternion(basis).Normalized();
        bufferWriter.WriteQuaternion(quaternion);

        var freebie = Random.Shared.Next(0, 5);
        bufferWriter.WriteInt(freebie, 0, 5);

        bufferWriter.FinalWrite(out _);
        var bufferReader = new BufferReader(buffer);

        // Vector2
        var readVector2 = bufferReader.ReadVector2(min, max, precision);
        AssertFloat(readVector2.X).IsEqualApprox(vector2.X, precision / 2);
        AssertFloat(readVector2.Y).IsEqualApprox(vector2.Y, precision / 2);

        // Vector2I
        var readVector2I = bufferReader.ReadVector2I((int)min, (int)max);
        AssertInt(readVector2I.X).IsEqual(vector2I.X);
        AssertInt(readVector2I.Y).IsEqual(vector2I.Y);
        
        // Rect2
        var readRect2 = bufferReader.ReadRect2(min, max, precision);
        AssertFloat(readRect2.Position.X).IsEqualApprox(rect2.Position.X, precision / 2);
        AssertFloat(readRect2.Position.Y).IsEqualApprox(rect2.Position.Y, precision / 2);
        AssertFloat(readRect2.Size.X).IsEqualApprox(rect2.Size.X, precision / 2);
        AssertFloat(readRect2.Size.Y).IsEqualApprox(rect2.Size.Y, precision / 2);
        
        // Rect2I
        var readRect2I = bufferReader.ReadRect2I((int)min, (int)max);
        AssertInt(readRect2I.Position.X).IsEqual(rect2I.Position.X);
        AssertInt(readRect2I.Position.Y).IsEqual(rect2I.Position.Y);
        AssertInt(readRect2I.Size.X).IsEqual(rect2I.Size.X);
        AssertInt(readRect2I.Size.Y).IsEqual(rect2I.Size.Y);
        
        // Vector3
        var readVector3 = bufferReader.ReadVector3(min, max, precision);
        AssertFloat(readVector3.X).IsEqualApprox(vector3.X, precision / 2);
        AssertFloat(readVector3.Y).IsEqualApprox(vector3.Y, precision / 2);
        AssertFloat(readVector3.Z).IsEqualApprox(vector3.Z, precision / 2);
        
        // Vector3I
        var readVector3I = bufferReader.ReadVector3I((int)min, (int)max);
        AssertInt(readVector3I.X).IsEqual(vector3I.X);
        AssertInt(readVector3I.Y).IsEqual(vector3I.Y);
        AssertInt(readVector3I.Z).IsEqual(vector3I.Z);
        
        // Basis
        var readBasis = bufferReader.ReadBasis(min, max, precision);
        AssertFloat(readBasis.X.X).IsEqualApprox(basis.X.X, precision / 2);
        AssertFloat(readBasis.X.Y).IsEqualApprox(basis.X.Y, precision / 2);
        AssertFloat(readBasis.X.Z).IsEqualApprox(basis.X.Z, precision / 2);
        AssertFloat(readBasis.Y.X).IsEqualApprox(basis.Y.X, precision / 2);
        AssertFloat(readBasis.Y.Y).IsEqualApprox(basis.Y.Y, precision / 2);
        AssertFloat(readBasis.Y.Z).IsEqualApprox(basis.Y.Z, precision / 2);
        AssertFloat(readBasis.Z.X).IsEqualApprox(basis.Z.X, precision / 2);
        AssertFloat(readBasis.Z.Y).IsEqualApprox(basis.Z.Y, precision / 2);
        AssertFloat(readBasis.Z.Z).IsEqualApprox(basis.Z.Z, precision / 2);
        
        // Transform3D
        var readTransform3D = bufferReader.ReadTransform3D(min, max, precision);
        AssertFloat(readTransform3D.Basis.X.X).IsEqualApprox(readBasis.X.X, precision / 2);
        AssertFloat(readTransform3D.Basis.X.Y).IsEqualApprox(transform3D.Basis.X.Y, precision / 2);
        AssertFloat(readTransform3D.Basis.X.Z).IsEqualApprox(transform3D.Basis.X.Z, precision / 2);
        AssertFloat(readTransform3D.Basis.Y.X).IsEqualApprox(transform3D.Basis.Y.X, precision / 2);
        AssertFloat(readTransform3D.Basis.Y.Y).IsEqualApprox(transform3D.Basis.Y.Y, precision / 2);
        AssertFloat(readTransform3D.Basis.Y.Z).IsEqualApprox(transform3D.Basis.Y.Z, precision / 2);
        AssertFloat(readTransform3D.Basis.Z.X).IsEqualApprox(transform3D.Basis.Z.X, precision / 2);
        AssertFloat(readTransform3D.Basis.Z.Y).IsEqualApprox(transform3D.Basis.Z.Y, precision / 2);
        AssertFloat(readTransform3D.Basis.Z.Z).IsEqualApprox(transform3D.Basis.Z.Z, precision / 2);
        AssertFloat(readTransform3D.Origin.X).IsEqualApprox(transform3D.Origin.X, precision / 2);
        AssertFloat(readTransform3D.Origin.Y).IsEqualApprox(transform3D.Origin.Y, precision / 2);
        AssertFloat(readTransform3D.Origin.Z).IsEqualApprox(transform3D.Origin.Z, precision / 2);
        
        // Vector4
        var readVector4 = bufferReader.ReadVector4(min, max, precision);
        AssertFloat(readVector4.X).IsEqualApprox(vector4.X, precision / 2);
        AssertFloat(readVector4.Y).IsEqualApprox(vector4.Y, precision / 2);
        AssertFloat(readVector4.Z).IsEqualApprox(vector4.Z, precision / 2);
        AssertFloat(readVector4.W).IsEqualApprox(vector4.W, precision / 2);
        
        // Vector4I
        var readVector4I = bufferReader.ReadVector4I((int)min, (int)max);
        AssertInt(readVector4I.X).IsEqual(vector4I.X);
        AssertInt(readVector4I.Y).IsEqual(vector4I.Y);
        AssertInt(readVector4I.Z).IsEqual(vector4I.Z);
        AssertInt(readVector4I.W).IsEqual(vector4I.W);
        
        // Plane
        var readPlane = bufferReader.ReadPlane(min, max, precision);
        AssertFloat(readPlane.D).IsEqualApprox(plane.D, precision / 2);
        AssertFloat(readPlane.Normal.X).IsEqualApprox(plane.Normal.X, precision / 2);
        AssertFloat(readPlane.Normal.Y).IsEqualApprox(plane.Normal.Y, precision / 2);
        AssertFloat(readPlane.Normal.Z).IsEqualApprox(plane.Normal.Z, precision / 2);
        
        // Aabb
        var readAabb = bufferReader.ReadAabb(min, max, precision);
        AssertFloat(readAabb.Position.X).IsEqualApprox(aabb.Position.X, precision / 2);
        AssertFloat(readAabb.Position.Y).IsEqualApprox(aabb.Position.Y, precision / 2);
        AssertFloat(readAabb.Position.Z).IsEqualApprox(aabb.Position.Z, precision / 2);
        AssertFloat(readAabb.Size.X).IsEqualApprox(aabb.Size.X, precision / 2);
        AssertFloat(readAabb.Size.Y).IsEqualApprox(aabb.Size.Y, precision / 2);
        AssertFloat(readAabb.Size.Z).IsEqualApprox(aabb.Size.Z, precision / 2);
        
        // Quaternion
        var readQuaternion = bufferReader.ReadQuaternion();
        AssertFloat(MathF.Abs(readQuaternion.X)).IsEqualApprox(MathF.Abs(quaternion.X), 0.001f / 2);
        AssertFloat(MathF.Abs(readQuaternion.Y)).IsEqualApprox(MathF.Abs(quaternion.Y), 0.001f / 2);
        AssertFloat(MathF.Abs(readQuaternion.Z)).IsEqualApprox(MathF.Abs(quaternion.Z), 0.001f / 2);
        AssertFloat(MathF.Abs(readQuaternion.W)).IsEqualApprox(MathF.Abs(quaternion.W), 0.001f / 2);
        
        var readFreebie = bufferReader.ReadInt(0, 5);
        AssertInt(readFreebie).IsEqual(freebie);
    }

    [TestCase]
    public void GuidReadAndWriteTest()
    {
        var buffer = new byte[16];
        var bufferWriter = new BufferWriter(buffer);
        var guid = Guid.CreateVersion7();
        bufferWriter.StoreOrWrite(guid.ToByteArray());
        
        var bufferReader = new BufferReader(buffer);
        var readGuid = new Guid(bufferReader.Read(16));
        AssertBool(readGuid == guid).IsTrue();
        AssertBool(readGuid.Version == 7).IsTrue();
    }
}