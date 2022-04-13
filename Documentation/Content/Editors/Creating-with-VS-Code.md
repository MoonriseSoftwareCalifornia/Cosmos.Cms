# Tutorial: Creating with VS Code ![VS Code Icon](https://github.com/CosmosSoftware/Cosmos.Cms/blob/main/Documentation/Content/Editors/vs-code-icon.png)

With Cosmos, you can develop rich web applications and content on your desktop and import them directly into Cosmos.

In this tutorial we show how to...

1. [Create a web page](#step-1-create-a-web-page-using-cosmos) in Cosmos then export it to your desktop.
2. [Build an example web app](#step-2-create-an-angular-web-app-and-build-it) using Visual Studio Code, Angular, and npm.
3. [Then import](#step-3-import-the-web-page) the build into Cosmos.

## Prerequisites

A few things need to be installed on your computer before starting this tutorial.

1 [Visual Studio Code](https://code.visualstudio.com)
2 [Node.js](https://nodejs.org)
3 [Angular CLI](https://angular.io/cli) using the following command `npm install -g @angular/cli`

Let's begin...
 
## Step 1: Create a web page using Cosmos
 
Start by creating a new web page with Cosmos that will house your web app.
 
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

## Step 2: Create an Angular web app and build it

Here we use the example application called [Building a template-driven form](https://angular.io/guide/forms#building-a-template-driven-form) from [Angular.io](https://angular.io/) website.

### Download, build and view the demo app

* [Download the source code](https://github.com/CosmosSoftware/Cosmos.Cms/blob/main/Documentation/Content/Editors/angular-app.zip) and open it with Visual Studio Code (VSC).
* Open a terminal in VSC and install the dependencies like this: `npm install`
* Now run the application with the command: `ng serve`
* The development web server will start, and soon in the terminal you will see the development server URL.
* Open the URL in a web browser.

Now you should see something like this:

![Example Web App Running](https://github.com/CosmosSoftware/Cosmos.Cms/blob/main/Documentation/Content/Editors/tutorial1-run-example.png)

If your application looks like above, all is good.  Notice that it does not include the "layout" elements of your Cosmos website?

Now we are going to start encorporating those elements into your application. When that is done we will import it into Cosmos.

### Encorporate the layout elements

* Locate the webpage you exported from Cosmos and add it to your project.
* You should now see the file added to your `src` folder like below (your file name might be different).

![Exported file now in src folder](https://github.com/CosmosSoftware/Cosmos.Cms/blob/main/Documentation/Content/Editors/tutorial1-file-added.png)

Open the exported file. Notice the layout content in the `<head> and <body>` is called out with comments.

These comments show where the layout content is injected, and where you can put your application content.

## Step 3: Import the web page

## Post Import Steps





Previous: [Create and Edit Content](https://github.com/CosmosSoftware/Cosmos.Cms/tree/main/Documentation/Content) Next: [Code Editor](https://github.com/CosmosSoftware/Cosmos.Cms/blob/main/Documentation/Content/Editors/CodeEditor.md) Releated: [HTML (WYSIWYG) Editor](https://github.com/CosmosSoftware/Cosmos.Cms/edit/main/Documentation/Content/Editors/WYSIWYG(HTMLEditor).md)
