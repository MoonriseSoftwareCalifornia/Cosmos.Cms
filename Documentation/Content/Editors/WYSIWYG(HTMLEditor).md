# WYSIWYG (HTML Editor)

Jump to:

* [How to define editable areas](#how-to-define-editable-areas)
* [How to open the editor](#how-to-open-the-editor)
* [How to use editing functions](#how-to-use)
* [Define what tools are available](#define-which-editor-tools-are-available-per-block)
* [Tryout the editor online](https://demos.telerik.com/kendo-ui/editor/all-tools)

Cosmos comes with two editors: the HTML Editor and Code Editor. Alternatively, you can develop with VS Code and import your work directly into Cosmos.  See [our documentation](https://github.com/CosmosSoftware/Cosmos.Cms/blob/main/Documentation/Content/Editors/Creating-with-VS-Code.md) on how to do that.

Below explains how to use the *HTML Editor*, also known as the "WYSIWYG Editor."

## What you see is what you get (WYSIWYG)
Cosmos' HTML editor is meant to enable the *non-technical person* to create and edit content on their own.

Most people are familiar with using word processors such as Google Docs, Microsoft Word and WordPerfect. Each is considered a "[WYSIWYG](https://en.wikipedia.org/wiki/WYSIWYG)" editors because as you write and stylize the page, it displays exactly how the document is going to print or be viewable to another on a computer.

WYSIWYG editors are so handly because you can style a document while you write it, and you do not have to be an expert to do so.

Cosmos comes with the [Kendo UI WYSIWYG editor](https://demos.telerik.com/kendo-ui/editor/index) that has some enhancements. It comes with many functions you may expect with word processors and you can test-drive those functions on the [product documentation website](https://demos.telerik.com/kendo-ui/editor/all-tools).

## How to define editable areas
The HTML editor isn't enabled for every web page by default.  It is setup to only edit those sections of a web page that have DIV tags "marked editable" by the [Code Editor](https://github.com/CosmosSoftware/Cosmos.Cms/blob/main/Documentation/Content/Editors/CodeEditor.md).

Here is an example of a DIV marked editable:

`<div contenteditable="true"><!-- Everything in here can be edited by the HTML editor --></div>`

*Please note:*

* *After the page is first saved, Cosmos converts the attribute "contenteditable" to a new attribute called "data-ccms-ceid" with a unique attribute value.  This new attribute not only marks editable blocks, it also assigns a unique ID to the block.*
* *Remove the attributes "contenteditable" and "data-ccms-ceid" to make a block no longer editable by the HTML editor.*


## How to open the editor
Start by either navigating to the page you want to modify using the "Editor" or by selecting the web page by choosing "Menu" then "Pages," then selecting "Edit" button by the page you want to work with.

This will open a list of "versions" of that page.

Look at the "Edit Tools" column. You should see a button for the "Code Editor," and if available, a button for the "HTML Editor." Click on the HTML Editor to edit.

If you do not see the HTML Editor button, the web page may not be [enabled for HTML editor use](#how-to-enable).
___
Previous: [Create and Edit Content](https://github.com/CosmosSoftware/Cosmos.Cms/tree/main/Documentation/Content) Next: [Code Editor](https://github.com/CosmosSoftware/Cosmos.Cms/blob/main/Documentation/Content/Editors/CodeEditor.md) Releated: [Creating with VS Code](https://github.com/CosmosSoftware/Cosmos.Cms/blob/main/Documentation/Content/Editors/Creating-with-VS-Code.md)

## How to Use

*Tip: See the [Kendo Editor product site](https://demos.telerik.com/kendo-ui/editor/all-tools) to try out the editor's functionality online.*

Upon first opening the HTML Editor you will see blocks on the page that have blue dashes around each like the screen shot below.

![HTML Editor no toolbar](https://github.com/CosmosSoftware/Cosmos.Cms/blob/main/Documentation/Content/Editors/html-editor-no-toolbar.png)

The HTML Editor Toolbar appears when an editable block is clicked on (see below).

![HTML Editor Toolbar](https://github.com/CosmosSoftware/Cosmos.Cms/blob/main/Documentation/Content/Editors/editor-toolbar.png)


## Define which editor tools are available per block

*Note: This is something usually done by a developer on behalf of a content author using the HTML editor.*

Sometimes it is wise to define the tools available to the HTML editor per block.  For example, you may what to limit what stylistic things people can do with content, not allow images to be embedded, or you may want to add tools to the default set already give.

Recall that blocks are made editable by marking them with the "contenteditable" attribute like this (this will contain the default set of editor tools):

`<div contenteditable='true'></div>`

To specify which editor tools are available for a block, create the attribute "ccms-editor-tools". The below example limits the editor to bold, italic and underline tools plus the Cosmos "insert tool" (always available) visible.

`<div contenteditable='true' ccms-editor-tools='bold,italic,underline'></div>`

### Default list of tools

Here is the complete list of "default" tools if none are define as above:

`bold, italic, underline, strikethrough, justifyLeft, justifyCenter, justifyRight, justifyFull, insertUnorderedList, insertOrderedList, insertUpperRomanList, insertLowerRomanList, indent, outdent, createLink, unlink, insertImag, insertFile, tableWizard, createTable, addRowAbove, addRowBelow, addColumnLeft, addColumnRight, deleteRow, deleteColumn, mergeCellsHorizontally", mergeCellsVertically, splitCellHorizontally, splitCellVertically, tableAlignLeft, tableAlignCenter, tableAlignRight, viewHtml, formatting, cleanFormatting, foreColor, backColor`

For more information regarding available tools, please see the [Kendo editor documentation](https://demos.telerik.com/aspnet-core/editor/all-tools).

___
Previous: [Create content](https://github.com/CosmosSoftware/Cosmos.Cms#create-content) Next: [Code editor](https://github.com/CosmosSoftware/Cosmos.Cms/blob/main/Documentation/Content/Editors/CodeEditor.md) Related: [Creating with VS Code](https://github.com/CosmosSoftware/Cosmos.Cms/blob/main/Documentation/Content/Editors/Creating-with-VS-Code.md)
