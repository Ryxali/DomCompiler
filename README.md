# Dom Compiler
Run as a command line process. It will package all .dme files into a single .dm file, and copy all referenced artwork to the compiled mod.

This enables you to split your mod into several files, gaining a better overview of its' contents. You can also integrate it with Visual Studio Code or other text processors to enable you to quickly iterate and deploy without working on it inside the Dominions mod directory.

When compiling, it will automatically detect all art references made in the mod and copy them to the output directory.

## Usage
DomCompiler arg0 [arg1] [--windex | --start-weapon-index] [--aindex | --start-armor-index] [--mindex | --start-monster-index] [--sindex | --start-spell-index] [--nindex | --start-nation-index] [--eindex | --start-eventcode-index]

Parses all .dme files in the working directory and outputs them to a single .dm file, copying all used art assets in the process.

**arg0**: path to the output file, as an absolute path or relative to arg1.

**arg1** (optional): path to working directory. Will use the default working directory if unspecified.

**--windex | --start-weapon-index** (optional): start index to use for relative ids for weapons (1000-3999).

**--aindex | --start-armor-index** (optional): start index to use for relative ids for armors (300-999).

**--mindex | --start-monster-index** (optional): start index to use for relative ids for monsters (5000-8999).

**--spindex | --start-spell-index** (optional): start index to use for relative ids for spells (1300-3999).

**--nindex | --start-nation-index** (optional): start index to use for relative ids for nations (159-499).

**--siindex | --start-site-index** (optional): start index to use for relative ids for sites (1700-3999)

**--enindex | --start-enchnbr-index** (optional): start index to use for relative ids for enchantment numbers (200-9999).

**--ecindex | --start-eventcode-index** (optional): start index to use for relative ids for event codes (-300--5000).


Indices default to their minimum absolute value, so 1000 for weapons, -300 for event codes, etc. Setting an id close to it's maximum value is not recommended, as ids can then overflow outside the recommended range.

### Example1 - Simple, Windows
`./DomCompiler.exe %appdata%/Dominions6/mods/MyMod.dm`
### Example2 - Specific source directory & weapon index, Windows
`./DomCompiler.exe %appdata%/Dominions6/mods/MyMod.dm C:/Users/Doe/Documents/MyMod --windex 1500`
## Other features
This compiler allows you to use local ids for weapons, armors, monsters, etc. so you can work with smaller, more graspable numbers for your entries. For instance a monster can defined as:
`#newmonster $1`
Instead of
`#newmonster 5000`
The compiler will then remap the '$1' to an index suitable for modded content. By default it will start at the minimum suggested from the modding manual, so for monsters $1 becomes 5000, $2 becomes 5001, etc.

This remapping can be configured by passing a specific index as an argument when compiling. With this you can define your own ranges of ids that your mod operates with. This is useful if you wish to ensure compatibility with other mods.

### Special Commands
A few special commands are available that can only be interprited by the compiler. It will transform the input to the corresponding output for .dm files. These commands accept relative ids, and are as such useful for targeting enchantments and monsters added by the mod.

In `#newspell` you can now type:

`##globalenchantment <ench nbr>` shorthand for setting the `#effect 10081` and `#damage <ench nbr>`

`##combatsummon <mnr>` shorthand for setting the `#effect 1` and `#damage <mnr>`

`##ritualsummon <mnr>` shorthand for setting the `#effect 10001` and `#damage <mnr>`

`##ritualsummoncom <mnr>` shorthand for setting the `#effect 10021` and `#damage <mnr>`

For commands specifying `<path>` or `<path mask>` a shorthand is now supported: 

`#magicskill F2G2` will give a mage 2 fire and 2 glamour magic.

`#magicskill DDDD` you can use numbers or repeat letters to define the magic. This gives a mage death 4.

`#custommagic FW 100` will give the mage 100% chance to get either fire or water magic.

Shorthand letters for paths are as follows:

- F = Fire
- A = Air
- W = Water
- E = Earth
- N = Nature
- D = Death
- S = Astral
- G = Glamour
- B = Blood
- H = Holy (Priest levels)

Lowercase is also supported, left to your preference.

## Getting started
This tool is executed from the command line, and is as such distributed bare-bones. Structuring the project and your developing environment is up to you. I've used Visual Studio Code for myself, which you can use as a base for your own mods. You can find this project [here](https://github.com/Ryxali/Dom-6-Dwarf-Faction).

## Limitations
When working with multiple files, the order they're output in the resulting mod file is difficult to control. If the order of specific commands are vital, put them in the same file.

## Contributing
For bugs, feature improvements, or other spicy opinions feel free to raise an issue [here](https://github.com/Ryxali/DomCompiler/issues).
