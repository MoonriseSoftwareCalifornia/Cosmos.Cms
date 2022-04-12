# Create and Edit Content

Three paths are availble for creating content that can be hosted with Cosmos.  You can use which one(s) work best for you. An overview of each is given here.

## [Code Editor](https://github.com/CosmosSoftware/Cosmos.Cms/blob/main/Documentation/Content/Editors/CodeEditor.md)

The code editor allows editing of HTML, JavaScript, CSS and TypeScript using the [Monaco Editor](https://microsoft.github.io/monaco-editor/), which "is the code editor that powers VS Code" ([Monaco Editor, Microsoft, 11 April, 2022](https://microsoft.github.io/monaco-editor/)).

This editor is best used by software engineers who are developing web pages that are rich JavaScript/CSS/HTML content for Cosmos.

Also use the code editor to "mark" `<div></div>` blocks as "content editable" for the [HTML Editor](https://github.com/CosmosSoftware/Cosmos.Cms/blob/main/Documentation/Content/Editors/WYSIWYG(HTMLEditor).md) like this:

`<div contenteditable="true"><!-- Everything in here can be edited by the HTML editor --></div>`

Here are some quick facts about the code editor:

* It is compatible with any HTML 5 modern web browser.
* Is is not compatible with mobile web browsers, or mobile devices (i.e. cell phones).
* Microsoft provides [documentation](https://code.visualstudio.com/docs/editor/editingevolved) on how to use this editor.

Read more: [Code Editor documentation](https://github.com/CosmosSoftware/Cosmos.Cms/blob/main/Documentation/Content/Editors/CodeEditor.md)

## [HTML Editor](https://github.com/CosmosSoftware/Cosmos.Cms/blob/main/Documentation/Content/Editors/WYSIWYG(HTMLEditor).md)

*Non-technical persons* to create and edit content using the [Kendo UI WYSIWYG editor](https://demos.telerik.com/kendo-ui/editor/index).  It comes with many functions you may expect with word processors and you can test-drive those functions on the [product documentation website](https://demos.telerik.com/kendo-ui/editor/all-tools).

Read more: [HTML editor documentation](https://github.com/CosmosSoftware/Cosmos.Cms/blob/main/Documentation/Content/Editors/WYSIWYG(HTMLEditor).md)

## Visual Studio Code

The best means to build web apps is to use Visual Studio Code with npm.  Angular, React, Vue and other applications can be built locally on your desktop computer, then imported into Cosmos for hosting.

Cosmos does not restrict the type of framework used. You can have multiple applications written in different frameworks all hosted on the same website. Choose the "best of breed" for your needs.
___
Previous: [Home](https://github.com/CosmosSoftware/Cosmos.Cms#cosmos) Next: [HTML Editor](https://github.com/CosmosSoftware/Cosmos.Cms/blob/main/Documentation/Content/Editors/WYSIWYG(HTMLEditor).md) Related: [Creating with VS Code](https://github.com/CosmosSoftware/Cosmos.Cms/blob/main/Documentation/Content/Editors/Creating-with-VS-Code.md)
