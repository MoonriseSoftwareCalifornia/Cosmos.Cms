# Code Editor

The code editor allows editing of HTML, JavaScript, CSS and TypeScript using the [Monaco Editor](https://microsoft.github.io/monaco-editor/), which "is the code editor that powers VS Code" ([Monaco Editor, Microsoft, 11 April, 2022](https://microsoft.github.io/monaco-editor/)).

This editor is best used by software engineers who are developing web pages that are rich JavaScript/CSS/HTML content for Cosmos.

Also use the code editor to "mark" `<div></div>` blocks as "content editable" for the [HTML Editor](https://github.com/CosmosSoftware/Cosmos.Cms/blob/main/Documentation/Content/Editors/WYSIWYG(HTMLEditor).md) like this:

`<div contenteditable="true"><!-- Everything in here can be edited by the HTML editor --></div>`

Here are some quick facts about the code editor:

* Is compatible with any HTML 5 modern web browser.
* Is NOT compatible with mobile web browsers or mobile devices (i.e. cell phones).
* Microsoft provides [documentation](https://code.visualstudio.com/docs/editor/editingevolved) on how to use this editor.
Previous: [Create and Edit Content](https://github.com/CosmosSoftware/Cosmos.Cms/tree/main/Documentation/Content) Next: [HTML (WYSIWYG) Editor](https://github.com/CosmosSoftware/Cosmos.Cms/blob/main/Documentation/Content/Editors/WYSIWYG(HTMLEditor).md) Releated: [Creating with VS Code](https://github.com/CosmosSoftware/Cosmos.Cms/blob/main/Documentation/Content/Editors/Creating-with-VS-Code.md)

## How to open the editor

Start by either navigating to the page you want to modify using the "Editor" or by selecting the web page by choosing "Menu" then "Pages," then selecting "Edit" button by the page you want to work with.

This will open a list of "versions" of that page.

Look at the "Edit Tools" column. You should see a button for the "Code Editor." Click on that to open the editor.

## Main tool bar

![Main tool bar](https://github.com/CosmosSoftware/Cosmos.Cms/blob/main/Documentation/Content/Editors/code-editor-top-tool-bar.png)

At the top of the Code Editor you will see a tool bar like the one above. Here is a synopsys of the available functions, working from left to right.

### Menu (Drop down)
* first is the main drop down menu. Here you can:
  * Save (saves page)
  * Close
  * Files (opens file manager)
  * Preview
  * Html (Opens the page in the HTML Editor)
  * Export (Exports the page to edit on your desktop)
  * Import (Imports the page content from your computer)

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

### Title Field

![Title field](https://github.com/CosmosSoftware/Cosmos.Cms/blob/main/Documentation/Content/Editors/editor-title.png)

The title field serves two purposes. It gives the web page a title and it also defines the URL path with the including of the forward slash `/`.

Here are a few tips:
* Keep titles as short as possible.
* Titles are automatically turned into valid URLs for the web page.
* Changing a title will also create an automatic redirect from the old URL to the new.
* If you change a title remember to save your edits.

## Shared (Multi-user) editing

The code editor allows for more then one person to be working on the same web page and version at a time.  For more information [please see our documentation](https://github.com/CosmosSoftware/Cosmos.Cms/blob/main/Documentation/Content/Editors/SharedEditing.md) regarind shared editing.
