using CDT.Cosmos.Cms.Common.Models;
using Kendo.Mvc.UI.Fluent;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;

namespace CDT.Cosmos.Cms.Models
{
    /// <summary>
    /// HTML editor model
    /// </summary>
    public class HtmlEditorModel : PageModel
    {
        /// <summary>
        /// Article view model
        /// </summary>
        public ArticleViewModel Article { get; set; }
        /// <summary>
        /// Tool set
        /// </summary>
        public ToolSetEnum ToolSet { get; set; }
        /// <summary>
        /// Edit mode is on
        /// </summary>
        public bool EditModeOn { get; private set; }
        /// <summary>
        /// Field name
        /// </summary>
        public string FieldName { get; private set; }
        /// <summary>
        /// Background color
        /// </summary>
        public string BackgroundColor { get; set; }
        /// <summary>
        /// Css class
        /// </summary>
        public string CssClass { get; set; }
        /// <summary>
        /// Editor tool factor
        /// </summary>
        public Action<EditorToolFactory> Tools { get; private set; }
        /// <summary>
        /// Build an instance of the model
        /// </summary>
        /// <param name="model"></param>
        /// <param name="fieldName"></param>
        /// <param name="editModeOn"></param>
        /// <param name="toolSet"></param>
        /// <param name="backgroundColor"></param>
        /// <param name="cssClass"></param>
        /// <returns></returns>
        public static HtmlEditorModel Build(ArticleViewModel model, string fieldName, bool editModeOn,
            ToolSetEnum toolSet, string backgroundColor = "white", string cssClass = "")
        {
            Action<EditorToolFactory> tools;
            switch (toolSet)
            {
                case ToolSetEnum.Full:
                    tools = tools => tools
                        .Clear()
                        .Bold().Italic().Underline().Strikethrough()
                        .JustifyLeft().JustifyCenter().JustifyRight().JustifyFull()
                        .InsertUnorderedList().InsertOrderedList()
                        .Outdent().Indent()
                        .CreateLink().Unlink()
                        .SubScript()
                        .SuperScript()
                        .TableEditing()
                        .ViewHtml()
                        .Formatting()
                        .CleanFormatting()
                        .FormatPainter()
                        .FontName()
                        .FontSize()
                        .ForeColor().BackColor()
                        .Print();
                    break;
                case ToolSetEnum.Map:
                case ToolSetEnum.Limited:
                    tools = tools => tools.Clear().ViewHtml().CleanFormatting();
                    break;
                case ToolSetEnum.Card:
                    tools = tools => tools.Clear().ViewHtml().CleanFormatting()
                        .CustomButton(c =>
                            c.Name("Icon Tool").Tooltip("Create a new card")
                                .Exec(@"function (e) { openCardEdtor($(this).data('kendoEditor')); }"));
                    break;
                default:
                    tools = tools => tools.Clear();
                    break;
            }

            return new HtmlEditorModel
            {
                Article = model,
                FieldName = fieldName,
                EditModeOn = editModeOn,
                Tools = tools,
                BackgroundColor = backgroundColor,
                CssClass = cssClass,
                ToolSet = toolSet
            };
        }
    }
}