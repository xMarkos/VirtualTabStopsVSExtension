# Feels like tabs

`Feels like tabs` is a Visual Studio 2022 extension that simplifies navigation in source code with keyboard when space indentation is used. Arrow key navigation, backspace, delete, and selection actions are modified to advance through whitespace as if the code contained tabs.

## Why would you want to use this extension

Many developers discussed whether spaces or tabs are superior and some might say that spaces won this battle. There is, however, a minority of developers (like me) who prefer tabs despite the outcome of this war. 

There are 2 major reasons why we will be using spaces instead of tabs:
1. We must respect code style of already established projects.
1. If you can't beat your enemy, it's usually better to join your enemy.

With the 2nd point in mind, I have switched to spaces instead of tabs, however, working with spaces drives me insane and here is why:
- When you press tab, Visual Studio automatically inserts spaces.
- When you want to delete this "fake tab" by pressing backspace, it won't work, it will delete only 1 of the (usually) 4 spaces inserted.
- When you press left or right arrow buttons, it will move the caret by just 1 space and not 4 spaces.

So the task of this extension is simple: pretend, that the source code contains tabs, when it encounters consecutive spaces, that is:
- When you press left/right arrows and all there is are just spaces, move the caret to the next tab stop.
- When you press backspace/delete, delete all spaces to the next tab stop.
- When you press shift + left/right arrows, extend the selection to the next tab stop.

## How to use

Just install the extension from the `vsix` file. There is no configuration - tab/spaces settings is taken from Visual Studio.

To turn the feature off, simply uninstall the extension.
