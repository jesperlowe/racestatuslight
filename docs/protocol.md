# Serial Protocol

The SimHub plugin sends one ASCII line per update:

```text
<MODE>,<SECTION>\n
```

Examples:

```text
R,7
Q,3
P,10
```

## Mode

| Mode | Meaning | Base color |
| --- | --- | --- |
| `R` | Race | Red |
| `Q` | Qualify / Qualification | Yellow |
| `P` | Practice / test / hotlap / fallback | Green |

Unknown/offline state is shown by the Arduino as dim white after startup until a valid command is received.

## Section

`SECTION` is an integer from `0` to `11` for the default 12 Nordschleife sections.

| Section | Track area |
| --- | --- |
| 0 | T13 / Hatzenbach |
| 1 | Hocheichen / Quiddelbacher Höhe |
| 2 | Flugplatz |
| 3 | Schwedenkreuz / Aremberg |
| 4 | Fuchsröhre |
| 5 | Adenauer Forst / Metzgesfeld |
| 6 | Kallenhard / Wehrseifen |
| 7 | Ex-Mühle / Bergwerk |
| 8 | Kesselchen / Klostertal |
| 9 | Karussell |
| 10 | Hohe Acht / Wippermann / Brünnchen |
| 11 | Pflanzgarten / Schwalbenschwanz / Döttinger Höhe / Galgenkopf |

## Mapping logic

First version uses linear lap progress:

```text
section = floor(trackProgress * 12)
```

The plugin clamps the result to `0..11`. If lap progress is unavailable, it falls back to sector index. If sector is also unavailable, it sends section `0`.

## 60 LED default table

```cpp
sectionStart = {0,5,10,15,20,25,30,35,40,45,50,55}
sectionEnd   = {4,9,14,19,24,29,34,39,44,49,54,59}
```
