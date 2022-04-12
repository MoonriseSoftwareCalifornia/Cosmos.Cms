# WYSIWYG (HTML Editor)

The Cosmos HTML editor enables the *non-technical persons* to create and edit content on their own. The following describes how to use the editor, and how to set a web page for it's use.

Jump to:

* [How to open the editor](#how-to-open-the-editor)
* [Main tool bar](#main-tool-bar)
  * [Edit mode indicator](https://github.com/CosmosSoftware/Cosmos.Cms/edit/main/Documentation/Content/Editors/WYSIWYG(HTMLEditor).md#edit-mode-indicator)
  * [How to publish page](#how-to-publishpublishing-status)
* [Editing Tools](#editing-tools)
* [Tryout the editor online](https://demos.telerik.com/kendo-ui/editor/all-tools)
* [How to define editable areas](#how-to-define-editable-areas)
* [Define what tools are available](#define-which-editor-tools-are-available-per-block)
* [Shared (multi user) editing](#shared-multi-user-editing)

Cosmos comes with two editors: the HTML Editor and Code Editor. Alternatively, you can develop with VS Code and import your work directly into Cosmos.  See [our documentation](https://github.com/CosmosSoftware/Cosmos.Cms/blob/main/Documentation/Content/Editors/Creating-with-VS-Code.md) on how to do that.

Below explains how to use the *HTML Editor*, also known as the "WYSIWYG Editor."

## What you see is what you get (WYSIWYG)

Most people are familiar with using word processors such as Google Docs, Microsoft Word and WordPerfect. Each is considered a "[WYSIWYG](https://en.wikipedia.org/wiki/WYSIWYG)" editors because as you write and stylize the page, it displays exactly how the document is going to print or be viewable to another on a computer.

Cosmos comes with the [Kendo UI WYSIWYG editor](https://demos.telerik.com/kendo-ui/editor/index) with some enhancements specific to Cosmos. It comes with many functions you may expect with word processors and you can test-drive those functions on the [product documentation website](https://demos.telerik.com/kendo-ui/editor/all-tools).

## How to open the editor
Start by either navigating to the page you want to modify using the "Editor" or by selecting the web page by choosing "Menu" then "Pages," then selecting "Edit" button by the page you want to work with.

This will open a list of "versions" of that page.

Look at the "Edit Tools" column. You should see a button for the "Code Editor," and if available, a button for the "HTML Editor." Click on the HTML Editor to edit.

If you do not see the HTML Editor button, the web page may not be [enabled for HTML editor use](#how-to-enable).
___
Previous: [Create and Edit Content](https://github.com/CosmosSoftware/Cosmos.Cms/tree/main/Documentation/Content) Next: [Code Editor](https://github.com/CosmosSoftware/Cosmos.Cms/blob/main/Documentation/Content/Editors/CodeEditor.md) Releated: [Creating with VS Code](https://github.com/CosmosSoftware/Cosmos.Cms/blob/main/Documentation/Content/Editors/Creating-with-VS-Code.md)

## Main tool bar

![HTML Editor main tool bar](https://github.com/CosmosSoftware/Cosmos.Cms/blob/main/Documentation/Content/Editors/html-editor-top-tool-bar.png)

At the top of the Code Editor you will see a tool bar like the one above. Here is a synopsys of the available functions, working from left to right.

### Menu (Drop down)
* The "Menu" button is a drop down menu with the following selections:
  * Save  (saves page)
  * Close
  * Files (opens file manager)
  * Preview
  * Code View (opens the page in the Code Editor)
### Edit mode indicator
This item will change color based on the edit status of this web page version.
* Green as shown above means the current user is editing the document and all others are locked out.
* Red indicates the file is locked for editing, it will also display user who has the file locked.
* Dark gray means the current item is not being edited by anyone.

### How to Publish/Publishing Status
Next there will be a button that displays the "publishing status" of the current version of the web page.
* While it says "Draft," this version is not viewable by the public.
* "Publishing" makes the version available for the public.
* To publish click the "Draft" open to open the dialog shwon below

![Publishing dialog](https://github.com/CosmosSoftware/Cosmos.Cms/blob/main/Documentation/Content/Editors/publishing-datetime-dialog.png)

Using this dialog choose the date and time in your local time zone when you want this version of the web page to go public.

Here are some tips:
* All date and times are set relative to the time zone shown. Above it is in Pacific Standard Time.
* To "un-publish" and item, simply click the "clear date/time" button.
* You can publish in the future by setting a future date and time.
* Setting the date and time to "now" or earlier will make the version viewable right away.

**IMPORTANT!** If you change publishing date/time it will NOT take effect until you save the web page.

## Editing Tools

*Tip: See the [Kendo Editor product site](https://demos.telerik.com/kendo-ui/editor/all-tools) to try out the editor's functionality online.*

Upon first opening the HTML Editor you will see blocks on the page that have blue dashes around each like the screen shot below.

![HTML Editor no toolbar](https://github.com/CosmosSoftware/Cosmos.Cms/blob/main/Documentation/Content/Editors/html-editor-no-toolbar.png)

The HTML Editor Toolbar appears when an editable block is clicked on (see below).

![HTML Editor Toolbar](https://github.com/CosmosSoftware/Cosmos.Cms/blob/main/Documentation/Content/Editors/editor-toolbar.png)

## Custom Insert Tool
On the toolbar you will notice the following button: ![Custom Insert Tool](https://github.com/CosmosSoftware/Cosmos.Cms/blob/main/Documentation/Content/Editors/html-editor-custom-insert-btn.png).

With this tool you can insert:
* A link to a web page or file uploaded to the website, or an external URL with the following custom properties:
  * CSS style(s)
  * CSS class(es)
  * Tool tip text
  * Open in another web page.
* Embed an uploaded image with the following properties:
  * Image alt text
  * CSS class(es)
  * CSS style(s)
  * Width and or Height value

### Insert Page URL/File URL/External URL
To use click on the plus button and the following dialog will appear:

![Insert link dialog](https://github.com/CosmosSoftware/Cosmos.Cms/blob/main/Documentation/Content/Editors/html-editor-custom-insert-dialog-link.png)

How to use Page/File/External URL Link Insert:
* Link to an existing page by typing the page title.
* Link to a file on the current website, click the plus button to open the file browser.
* To link to an external URL, simply paste the URL in the top text box.
* Then fill out the rest of the dialog as necessary.
* Click "Insert" when done.

### Insert Image
Not click on the "Insert Image" tab.

![Insert image dialog](https://github.com/CosmosSoftware/Cosmos.Cms/blob/main/Documentation/Content/Editors/html-editor-custom-insert-dialog-img.png)

How to use:
* Link to an image uploaded to the current website by clicking the plus button. This will open the file browser.
* Fill out the rest of the dialog as necessary.
* Click "Insert" when done.

## How to define editable areas
The HTML editor isn't enabled for every web page by default.  It is setup to only edit those sections of a web page that have DIV tags "marked editable" by the [Code Editor](https://github.com/CosmosSoftware/Cosmos.Cms/blob/main/Documentation/Content/Editors/CodeEditor.md).

Here is an example of a DIV marked editable:

`<div contenteditable="true"><!-- Everything in here can be edited by the HTML editor --></div>`

*Please note:*

* *After the page is first saved, Cosmos converts the attribute "contenteditable" to a new attribute called "data-ccms-ceid" with a unique attribute value.  This new attribute not only marks editable blocks, it also assigns a unique ID to the block.*
* *Remove the attributes "contenteditable" and "data-ccms-ceid" to make a block no longer editable by the HTML editor.*

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

For more information regarding *all the available tools*, please see the [Kendo editor documentation](https://demos.telerik.com/aspnet-core/editor/all-tools).

## Shared (Multi-user) Editing

The HTML editor allows for more then one person to be working on the same web page and version at a time.  For more information [please see our documentation](https://github.com/CosmosSoftware/Cosmos.Cms/blob/main/Documentation/Content/Editors/SharedEditing.md) regarind shared editing.

___
Previous: [Create content](https://github.com/CosmosSoftware/Cosmos.Cms#create-content) Next: [Code editor](https://github.com/CosmosSoftware/Cosmos.Cms/blob/main/Documentation/Content/Editors/CodeEditor.md) Related: [Creating with VS Code](https://github.com/CosmosSoftware/Cosmos.Cms/blob/main/Documentation/Content/Editors/Creating-with-VS-Code.md)
