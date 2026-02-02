using System;
using CityRush.World.Map.Runtime;

namespace CityRush.World.Addresses
{
    /// <summary>
    /// Deterministic identity for a Street instance in the world grid.
    /// Key fields: Position + StreetId.
    /// </summary>
    public readonly struct StreetAddress : IEquatable<StreetAddress>
    {
        public readonly MapPosition Position;
        public readonly string StreetId;

        public StreetAddress(MapPosition position, string streetId)
        {
            Position = position;
            StreetId = streetId ?? string.Empty;
        }

        public bool Equals(StreetAddress other)
        {
            return Position.ZoneIndex == other.Position.ZoneIndex
                   && Position.Row == other.Position.Row
                   && Position.Col == other.Position.Col
                   && string.Equals(StreetId, other.StreetId, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is StreetAddress other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = (hash * 31) + Position.ZoneIndex;
                hash = (hash * 31) + Position.Row;
                hash = (hash * 31) + Position.Col;
                hash = (hash * 31) + (StreetId != null ? StringComparer.Ordinal.GetHashCode(StreetId) : 0);
                return hash;
            }
        }

        public static bool operator ==(StreetAddress left, StreetAddress right) => left.Equals(right);
        public static bool operator !=(StreetAddress left, StreetAddress right) => !left.Equals(right);

        public override string ToString()
        {
            return $"{StreetId} [Z{Position.ZoneIndex} R{Position.Row} C{Position.Col}]";
        }
    }

    /// <summary>
    /// Deterministic identity for a Building instance on a Street.
    /// Key fields: Street + BuildingIndex.
    /// </summary>
    public readonly struct BuildingAddress : IEquatable<BuildingAddress>
    {
        public readonly StreetAddress Street;
        public readonly int BuildingIndex;   // 0-based, stable
        public readonly int BuildingNumber;  // display / UI (default: index+1)

        public BuildingAddress(StreetAddress street, int buildingIndex, int buildingNumber)
        {
            Street = street;
            BuildingIndex = buildingIndex;
            BuildingNumber = buildingNumber;
        }

        public bool Equals(BuildingAddress other)
        {
            return Street.Equals(other.Street) && BuildingIndex == other.BuildingIndex;
        }

        public override bool Equals(object obj)
        {
            return obj is BuildingAddress other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = (hash * 31) + Street.GetHashCode();
                hash = (hash * 31) + BuildingIndex;
                return hash;
            }
        }

        public static bool operator ==(BuildingAddress left, BuildingAddress right) => left.Equals(right);
        public static bool operator !=(BuildingAddress left, BuildingAddress right) => !left.Equals(right);

        public override string ToString()
        {
            return $"{Street} / B#{BuildingNumber} (idx {BuildingIndex})";
        }
    }

    /// <summary>
    /// Deterministic identity for an Apartment (door) inside a Building.
    /// Key fields: Building + FloorIndex + ApartmentIndexOnFloor.
    /// Display number: ApartmentNumber = FloorIndex * ApartmentsPerFloor + ApartmentIndexOnFloor + 1
    /// </summary>
    public readonly struct ApartmentAddress : IEquatable<ApartmentAddress>
    {
        public readonly BuildingAddress Building;
        public readonly int FloorIndex;              // 0-based
        public readonly int ApartmentIndexOnFloor;   // 0-based
        public readonly int ApartmentNumber;         // 1-based, across whole building

        public ApartmentAddress(BuildingAddress building, int floorIndex, int apartmentIndexOnFloor, int apartmentsPerFloor)
        {
            Building = building;
            FloorIndex = floorIndex;
            ApartmentIndexOnFloor = apartmentIndexOnFloor;

            int apf = apartmentsPerFloor <= 0 ? 1 : apartmentsPerFloor;
            ApartmentNumber = (floorIndex * apf) + apartmentIndexOnFloor + 1;
        }

        public bool Equals(ApartmentAddress other)
        {
            return Building.Equals(other.Building)
                   && FloorIndex == other.FloorIndex
                   && ApartmentIndexOnFloor == other.ApartmentIndexOnFloor;
        }

        public override bool Equals(object obj)
        {
            return obj is ApartmentAddress other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = (hash * 31) + Building.GetHashCode();
                hash = (hash * 31) + FloorIndex;
                hash = (hash * 31) + ApartmentIndexOnFloor;
                return hash;
            }
        }

        public static bool operator ==(ApartmentAddress left, ApartmentAddress right) => left.Equals(right);
        public static bool operator !=(ApartmentAddress left, ApartmentAddress right) => !left.Equals(right);

        public override string ToString()
        {
            return $"{Building} / Apt#{ApartmentNumber} (F{FloorIndex} i{ApartmentIndexOnFloor})";
        }
    }
}