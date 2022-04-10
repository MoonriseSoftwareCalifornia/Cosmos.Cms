# Create a Layout
The following shows how to create your own layout. If you are not familar with how layouts are used, please see our [documentation](https://github.com/CosmosSoftware/Cosmos.Cms/blob/main/Documentation/Layouts/About.md) before getting started.

## Prerequisits
The following will be needed before proceeding:
* [Cosmos is installed](https://github.com/CosmosSoftware/Cosmos.Cms/blob/main/Documentation/Installation/AzureClickInstall.md)
* [Post installation steps](https://github.com/CosmosSoftware/Cosmos.Cms/blob/main/Documentation/Installation/AzureClickInstall.md#required-post-installation-steps) are completed.
* You have basic familiarity with CSS, HTML and JavaScript
* You are able to log into Cosmos as an administrator

## Plan Your Layout
Before deciding on look and feel, colors and fonts, and so forth, make some decisions about how you want your website structured. 

Here are some things to consider--each will affect how your layout is built:

* Will your website have common elements appear on each page, examples include:
  *  Banner
  *  Navigation bar
  *  Footer
  *  Google Analytics or other tools
*  Do you have a certain framework you want to use such as Bootstrap or other?
*  Will you require custom JavaScript and/or CSS to appear on each page?

Here are further tips in planning your layout:

* Keep it lean, and only include what is absolutely necessary to appear on each web page.
* If your Cosmos site will host web applications, create a layout that uses "plain JavaScript and CSS." This will reduce conflicts between your layout and each page.
* Cosmos does not require any kind of framework. Use one you are most familiar with.

## Step by Step
While you can design your layout as you build it, it is best to make certain decisions before you start.  Each isn't really specific to Cosmos as it applies to all website design:

1. Log into Cosmos, and from the drop down menu in the upper left, choose "Layouts."
2. Click the "Create New" button.
3. The code editor will open.

Notice tabs at the top named "Head," "Body Attributes," "Header Content," and "Footer Content."  Click on each and you will notice they change.  Right now, because this is a new layout, each will be blank. This is where your [decisions above](https://github.com/CosmosSoftware/Cosmos.Cms/blob/main/Documentation/Layouts/Create.md#plan-your-layout) come into play.

### Head
Click on the "Head" tab.  Here place links to style sheets or JavaScript libraries, Google Analytics snippets, and any other things you would like to appear within the `<head></head>` on every web page in your website.

### Body Attributes
Sometimes you may want the `<body></body>` to have attributes that you cannot set with CSS or easily with JavaScript. In such a case you can add attributes that will be injected into the body tag. If this isn't the case, leave this blank.

Here is an example: `myattribute="somevalue" anotherattribute="anothervalue"`

### Header Content
The body "header content" is injected at the top of the `<body></body>` on every page of your website. This might be a banner or perhaps a navigation bar. Leave this blank if you do not plan to have a banner or navigation bar.
