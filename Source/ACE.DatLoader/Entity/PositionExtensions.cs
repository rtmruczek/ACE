﻿using ACE.Entity;

namespace ACE.DatLoader.Entity
{
    /// <summary>
    /// Helper function to read position data from dat files
    /// </summary>
    public static class PositionExtensions
    {
        public static Position ReadPosition(DatReader datReader)
        {
            Position p = new Position();
            p.Read(datReader);
            return p;
        }

        public static void Read(this Position p, DatReader datReader)
        {
            p.PositionX = datReader.ReadSingle();
            p.PositionY = datReader.ReadSingle();
            p.PositionZ = datReader.ReadSingle();
            p.RotationW = datReader.ReadSingle();
            p.RotationX = datReader.ReadSingle();
            p.RotationY = datReader.ReadSingle();
            p.RotationZ = datReader.ReadSingle();
        }
    }
}
