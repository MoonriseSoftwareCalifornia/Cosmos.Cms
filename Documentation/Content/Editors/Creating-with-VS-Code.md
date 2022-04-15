# Tutorial: Creating with VS Code ![VS Code Icon](https://github.com/CosmosSoftware/Cosmos.Cms/blob/main/Documentation/Content/Editors/vs-code-icon.png)

With Cosmos, you can develop rich web applications and content on your desktop and import them directly into Cosmos.

In this tutorial I will show how to...

1. [Create a web page](#step-1-create-a-web-page-using-cosmos) in Cosmos then export it to your desktop.
2. [Build an example web app](#step-2-create-an-angular-web-app-and-build-it) using Visual Studio Code, Angular, and npm.
3. [Then import](#step-3-import-the-web-page) the build into Cosmos.

## Prerequisites

A few things need to be installed on your computer before starting this tutorial.

1 [Visual Studio Code](https://code.visualstudio.com)
2 [Node.js](https://nodejs.org)

Let's begin...
 
## Step 1: Create an React web app

This example a React project called [Snap Shot](https://github.com/Yog9/SnapShot) found on [GitHub](https://github.com/Yog9/SnapShot).

It uses the following:

* React js
* React Router
* React Hooks
* Context API
* Flickr API

Follow the [getting started](https://github.com/Yog9/SnapShot#getting-started) instructions on the [Readme document](https://github.com/Yog9/SnapShot#snap-shot-) to build download, install and build your application.

When you run this app you should see something like below.

![Example Web App Running](https://github.com/CosmosSoftware/Cosmos.Cms/blob/main/Documentation/Content/Editors/SnapShotDemo.png)

Now we are going to get this application ready to import into Cosmos.

## Step 2: Add layout elements

A keen eye will notice that the application above does not include the "layout" elements of my [Cosmos website](https://cosmos.moonrise.net).

Specifically, the application is missing the header and footer are missing.

Before diving into building your React app, we need to create a web page on Comsos that will house your application.

Start by creating a new web page:
 
 * Click the "Menu" button and select "Pages."
 * From the page list page, click the "New Page" button.
 * You will see the following dialog:

![Create page dialog](https://github.com/CosmosSoftware/Cosmos.Cms/blob/main/Documentation/Content/Editors/create-page-dialog.png)

 * In the above dialog give the page a title. For now use "My App".
 * There is no need to choose a page template, so click "Create" and you should see the HTML editor open it up.

![New Lorem Ipsum Page](https://github.com/CosmosSoftware/Cosmos.Cms/blob/main/Documentation/Content/Editors/new-lorem-ipsom-page.png)

 * Click the "Menu" button and "Save."
 * Then, click "Menu" then "Code View" to open the code editor.
 * Next, click the "Code Editor" button and click "Export."

This will download a web page ready that you will need later. Remember where you downloaded it.

## Step 2:

* Locate the webpage you exported from Cosmos and add it to your project.
* You should now see the file added to your `src` folder like below (your file name might be different).

![Exported file now in src folder](https://github.com/CosmosSoftware/Cosmos.Cms/blob/main/Documentation/Content/Editors/tutorial1-file-added.png)

*Tip: Before doing anything else, open the exported file in a web browser to make sure everything displays as expected.

Open the exported file in VS Code. Notice the layout content in the `<head> and <body>` is called out with HTML comments, and also the HTML comments that show where you can put your application content on the page.

Let's take a closer look...

### HEAD editable area
Look inside the `<head></head>` for the comments that indicate where you can put your page-specific code that belongs in the HEAD.

![<!--  BEGIN: Cosmos Layout HEAD content inject (not editable). --><!--  END: Cosmos HEAD inject (not editable). -->](https://github.com/CosmosSoftware/Cosmos.Cms/blob/main/Documentation/Content/Editors/tutorial1-export-head-editable.png)

IMPORTANT: As you place your page-specific HEAD code between the comments shown above, **do not delete the comments.** The import processes needs thes present to work.

### BODY editable area
Using VS Code scroll down the exported page and find the comments indicated where you can place the code for your web application that needs to go inside the `<body></body>`.

![<!-- BEGIN: Page specific BODY content goes here (editable) --><!-- END: Page specific BODY content (editable) -->](https://github.com/CosmosSoftware/Cosmos.Cms/blob/main/Documentation/Content/Editors/tutorial1-export-body-editable.png)

Place the bulk of your application code here that belongs in the BODY. Like before, **do not delete** the HTML comments elsewise the import process will break.

### Below footer (bottom of BODY) editable area
The last opportunity to place page-specific code is after the layout footer and at the bottom of the `<body></body>`. Look for the following HTML comment:

![<!-- BEGIN: Page specific *end* of BODY content goes here (editable) --><!-- END: Page specific *end* of BODY content  (editable) -->](https://github.com/CosmosSoftware/Cosmos.Cms/blob/main/Documentation/Content/Editors/tutorial1-export-footer-editable.png)

Insert your end-of-page content here, and as before **do not delete** the HTML comments.

## Step 3: Import the web page

## Post Import Steps





Previous: [Create and Edit Content](https://github.com/CosmosSoftware/Cosmos.Cms/tree/main/Documentation/Content) Next: [Code Editor](https://github.com/CosmosSoftware/Cosmos.Cms/blob/main/Documentation/Content/Editors/CodeEditor.md) Releated: [HTML (WYSIWYG) Editor](https://github.com/CosmosSoftware/Cosmos.Cms/edit/main/Documentation/Content/Editors/WYSIWYG(HTMLEditor).md)
