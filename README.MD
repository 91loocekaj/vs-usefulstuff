-----Sluice Details-----
- The sluice takes from the vanilla panning system so any block added from another mod should work.
- The sluice efficiency and speed can be changed in the config.
- The inventory of the sluice is limited to blocks with the "pannable" attribute and will drop something that does not have it.

-----Shield Details-----
- The shield item does not truly do anything, rather it is the shield behavior patched to the player that activates when the proper conditions are met:
- The item is in a active hotbar slot, the player is crouching, the damage comes from an entity, and the entity is in front of the player
- Damage to the shield is calculated by the tier of the damage, the damage itself, and the tier of the Shield

-----World Gen Changes-----
With the world generation changes activated in config, a Harmony patch increases cave sizes by 50% and makes it so that they are always larger near the bottom. With the added mode they also become bigger by making them branch more. This is a WIP and a suitable replacement will be made in the Terrain Generation instead as that is much more efficient.

-----Config Options-----
SluiceEfficiency: A float that determines the percentage of how high the chance of getting drops from a block is
SluiceSiftTime: A float that determines how much time it takes to process a block. 1 is one in game hour
CrazyCaves: A bool that when true enables the cave changes
CrazyCavesInsanityMode: A bool which makes caves significantly bigger.
