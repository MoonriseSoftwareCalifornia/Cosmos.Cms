# WYSIWYG (HTML Editor)

Cosmos comes with two editors: the HTML Editor and Code Editor. Alternatively, you can develop with VS Code and import directly into Cosmos.  See [our documentation](https://github.com/CosmosSoftware/Cosmos.Cms/blob/main/Documentation/Content/Editors/Creating-with-VS-Code.md) on how to do that.

Below explains how to use the *HTML Editor*, also known as the "WYSIWYG Editor."

## What you see is what you get (WYSIWYG)
Cosmos' HTML editor is meant to enable the *non-technical person* to create and edit content on their own.

Most people are familiar with using word processors such as Google Docs, Microsoft Word and WordPerfect. Each is considered a "[WYSIWYG](https://en.wikipedia.org/wiki/WYSIWYG)" editors because as you write and stylize the page, it displays exactly how the document is going to print or be viewable to another on a computer.

WYSIWYG editors are so handly because you can style a document while you write it, and you do not have to be an expert to do so.

Cosmos comes with the [Kendo UI WYSIWYG editor](https://demos.telerik.com/kendo-ui/editor/index) that has some enhancements. It comes with many functions you may expect with word processors and you can test-drive those functions on the [product documentation website](https://demos.telerik.com/kendo-ui/editor/all-tools).

### How to Enable
The HTML editor isn't enabled for every web page by default.  It is setup to only edit those sections of a web page that have `<div></div>` blocks "marked editable" by the [Code Editor](https://github.com/CosmosSoftware/Cosmos.Cms/blob/main/Documentation/Content/Editors/CodeEditor.md).

Here is an example of a DIV marked editable:

`<div contenteditable="true"><!-- Everything in here can be edited by the HTML editor --></div>`

### How to open the editor
Start by either navigating to the page you want to modify using the "Editor" or by selecting the web page by choosing "Menu" then "Pages," then selecting "Edit" button by the page you want to work with.

This will open a list of "versions" of that page.

Look at the "Edit Tools" column. You should see a button for the "Code Editor," and if available, a button for the "HTML Editor." Click on the HTML Editor to edit.

If you do not see the HTML Editor button, the web page may not be [enabled for HTML editor use](#how-to-enable).
___
Previous: [Create and Edit Content](https://github.com/CosmosSoftware/Cosmos.Cms/tree/main/Documentation/Content) Next: [Code Editor](https://github.com/CosmosSoftware/Cosmos.Cms/blob/main/Documentation/Content/Editors/CodeEditor.md) Releated: [Creating with VS Code](https://github.com/CosmosSoftware/Cosmos.Cms/blob/main/Documentation/Content/Editors/Creating-with-VS-Code.md)
