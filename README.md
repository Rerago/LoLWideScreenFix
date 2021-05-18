# LoLWideScreenFix

Mod for League of Legends to improve (ultra)wide screen support.

# How it works

How the whole thing works can be read in a [blog entry](https://www.doppnet.com/hacking-league-of-legends-hud.html) by [@tnajdek](https://github.com/tnajdek/). I did not do more than rewrite the whole thing in C# and get it running on the current patch. So thanks again to [@tnajdek](https://github.com/tnajdek/) for his work.

# How to use

LoLWideScreenFix is called as follows:

```LoLWideScreenFix -l <path_to_lol> -o <output_path> -t <target_resolution> -m <mode>```

### Parameters:

  * ```-l``` |  ```-leaguepath```: Path to Leagues of Legends folder (e.g. ```C:\Riot Games\League of Legends\```)
  * ```-o``` |  ```-outputpath```: Path where the result should be generated (e.g. ```D:\lolcustomskin-tools-64\installed\```)
  * ```-t``` |  ```-targetres```: Target resolution width (e.g. ```1920```)
  * ```-m``` |  ```-outputmode```: Mode in which the output should be done (e.g. ```RAW_MOD_FOLDER``` (or just ```1```) or ```LOLCUSTOMSKIN_MOD``` (or just ```2```))

#### Target resolution width

The Target resolution width specifies the value to which the UI width should be limited. The value is freely selectable (as long as it is greater than 1440). However, ``1920`` is a very good value to approach your desired value.

#### Output modes

Currently, two modes are supported. 

* RAW folder: Outputs the modified files unpacked in a folder
* Mod for LoLCustomSkin: Create a folder that can be copied and used directly in the "installed" folder of LoLCustomSkin.

| Output mode                       | Parameter                                |
|-----------------------------------|------------------------------------------|
| Output as RAW folder              | ```-m 1``` or ```-m RAW_MOD_FOLDER```    |
| Output as a mod for LoLCustomSkin | ```-m 2``` or ```-m LOLCUSTOMSKIN_MOD``` | 

How e.g. LoLCustomSkin (LCS Manager) works I do not explain now further. This can be seen in countless Youtube video.

# Current status

Everything works very well so far. However, not everything is 100% supported, such as the minimap on the left side.

# Dear Riot

I love LoL your game (despite the fact that I'm pretty bad at it). I have recently joined the [r/ultrawidemasterrace/](https://www.reddit.com/r/ultrawidemasterrace/). We are roughly 12% of all players according to the [Steam Hardware Software Survey](https://store.steampowered.com/hwsurvey/Steam-Hardware-Software-Survey-Welcome-to-Steam). So a lot to consider.

League of Legends supports resolutions very well for which I wanted to say thanks. However, some UI elements are aligned left and right on the edge. It would be wonderful if a support for a centrallized UI could be built directly into League of Legends.
