using CityRush.Items;
using System;
using System.Collections.Generic;
public sealed class ItemsDb
{
    private readonly Dictionary<int, ItemDefinition> _byId;


    public int Version { get; }
    public int Count => _byId.Count;


    private ItemsDb(int version, Dictionary<int, ItemDefinition> byId)
    {
        Version = version;
        _byId = byId;
    }


    public bool TryGet(int itemId, out ItemDefinition def)
    {
        return _byId.TryGetValue(itemId, out def);
    }


    public static bool TryCreateFromDto(ItemsDbDto dto, out ItemsDb db, out string error)
    {
        db = null;
        error = null;


        if (dto == null)
        {
            error = "ItemsDbDto is null (JSON parse failed).";
            return false;
        }


        if (dto.items == null)
        {
            error = "ItemsDbDto.items is null.";
            return false;
        }


        var dict = new Dictionary<int, ItemDefinition>(dto.items.Length);


        for (int i = 0; i < dto.items.Length; i++)
        {
            ItemDto it = dto.items[i];
            if (it == null) continue;


            if (it.itemId <= 0)
            {
                error = $"Invalid itemId at index {i}: {it.itemId}";
                return false;
            }


            if (dict.ContainsKey(it.itemId))
            {
                error = $"Duplicate itemId {it.itemId} (index {i}).";
                return false;
            }


            int maxStack = it.maxStack;
            if (maxStack <= 0)
                maxStack = 1;


            ItemDefinition.WeaponData weapon = null;
            if (it.weapon != null)
            {
                string weaponDefId = it.weapon.weaponDefinitionId;
                if (!string.IsNullOrWhiteSpace(weaponDefId))
                    weapon = new ItemDefinition.WeaponData(weaponDefId);
            }


            dict.Add(
            it.itemId,
            new ItemDefinition(
            it.itemId,
            it.name,
            it.category,
            it.rarity,
            it.iconKey,
            maxStack,
            weapon
            )
            );
        }


        db = new ItemsDb(dto.version, dict);
        return true;
    }
}