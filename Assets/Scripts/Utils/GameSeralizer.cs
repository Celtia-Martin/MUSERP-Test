using Muse_RP.Exceptions;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class GameSeralizer
{
    #region To Bytes
    public static byte[] positionInfoToBytes(Vector2 position, int id)
    {
        List<byte> buffer = new List<byte>();
        buffer.AddRange(BitConverter.GetBytes(id));
        buffer.AddRange(BitConverter.GetBytes(position.x));
        buffer.AddRange(BitConverter.GetBytes(position.y));

        return buffer.ToArray();
    }

    public static byte[] newCharacterToBytes(Color color, int id)
    {
        List<byte> buffer = new List<byte>();
        buffer.AddRange(BitConverter.GetBytes(id));
        buffer.AddRange(BitConverter.GetBytes(color.r));
        buffer.AddRange(BitConverter.GetBytes(color.g));
        buffer.AddRange(BitConverter.GetBytes(color.b));

        return buffer.ToArray();
    }

    public static byte[] pointsToBytes(int id, int points)
    {
        List<byte> buffer = new List<byte>();
        buffer.AddRange(BitConverter.GetBytes(id));
        buffer.AddRange(BitConverter.GetBytes(points));

        return buffer.ToArray();
    }
    public static byte[] enemyDisappearToBytes(int id)
    {
        return BitConverter.GetBytes(id);
    }
    public static byte[] spawnEnemyToBytes(int index, ushort type)
    {
        List<byte> buffer = new List<byte>();
        buffer.AddRange(BitConverter.GetBytes(index));
        buffer.AddRange(BitConverter.GetBytes(type));
        return buffer.ToArray();
    }
    #endregion
    #region Get Information From Bytes
    public static Color getCharacterFromBytes(byte[] data, out int ID)
    {
        if (data.Length < 16)
        {
            // throw new IncorrectMessageFormatException();
            ID = -1;
            return Color.white;
        }
        ID = BitConverter.ToInt32(data, 0);
        Color color = new Color();
        color.r = BitConverter.ToSingle(data, 4);
        color.g = BitConverter.ToSingle(data, 8);
        color.b = BitConverter.ToSingle(data, 12);

        return color;
    }
    public static Vector2 getPositionFromBytes(byte[] data, out int ID)
    {
        if (data.Length < 12)
        {
            ID = -1;
            return Vector2.zero;
            //  throw new IncorrectMessageFormatException();
        }

        ID = BitConverter.ToInt32(data, 0);
        Vector2 position = new Vector2();
        position.x = BitConverter.ToSingle(data, 4);
        position.y = BitConverter.ToSingle(data, 8);

        return position;
    }

    public static int getPointsFromBytes(byte[] data, out int ID)
    {
        if (data.Length < 8)
        {
            ID = -1;
            return -1;
            //throw new IncorrectMessageFormatException();
        }
        int points;
        ID = BitConverter.ToInt32(data, 0);
        points = BitConverter.ToInt32(data, 4);
        return points;
    }

    public static int getDisappearEnemyFromBytes(byte[] data)
    {
        if (data.Length < 4)
        {
            return -1;
            throw new IncorrectMessageFormatException();
        }
        return BitConverter.ToInt32(data, 0);
    }
    public static int getSpawnEnemyFromBytes(byte[] data, out ushort type)
    {
        if (data.Length < 6)
        {
            type = 52;
            return -1;
        }
        //throw new IncorrectMessageFormatException();
        int index = BitConverter.ToInt32(data, 0);
        type = BitConverter.ToUInt16(data, 4);
        return index;

    }

    #endregion





}
