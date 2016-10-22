# Wang Tiles
============

Implements a random map generator using Wang Tiles, in C#.

* Author: [Sérgio Flores](https://github.com/relfos)
* License: [MIT](https://opensource.org/licenses/MIT)
* [Reporting Issues](https://github.com/relfos/WangTiles/issues)
* Support can be obtained via [Email](mailto:sergio.flores@lunarlabs.pt)
* If you require some specific feature please contact for a quote.

============
Wang Tiles are a very simple but useful concept that can be used to generate an infinite set of connecting tiles.
This small project was implemented based on the information found [here](http://s358455341.websitehome.co.uk/stagecast/wang/intro.html)
Note that is just a small proof of concept done in 1 hour, using C# and OpenTK for rendering the output (you can easily swap it out for any other rendering system, the algoritm only gives you a list of tile IDs).

A sample output, using a simple tileset with roads.
![Sample Output](/wang1.png)

============
Same map, using a different tileset that shows tile IDs.
![Numbers Output](/wang2.png)
