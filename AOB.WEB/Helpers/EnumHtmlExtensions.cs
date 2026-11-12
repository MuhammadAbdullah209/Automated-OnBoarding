using System.Net;
using System.Text;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AOB.WEB.Helpers
{
    /// <summary>One choice in a multi-select dropdown.</summary>
    /// <param name="Value">The value posted when ticked.</param>
    /// <param name="Label">Text shown in the list and on the chip.</param>
    /// <param name="Selected">Ticked when the page opens.</param>
    /// <param name="Disabled">Shown but cannot be ticked (and is not posted).</param>
    /// <param name="Badge">Small tag shown after the label, e.g. "Coming soon" or "Optional".</param>
    /// <param name="Hint">Smaller helper text shown under the label.</param>
    public record MultiSelectItem(
        string Value, string Label, bool Selected = false, bool Disabled = false, string? Badge = null, string? Hint = null);

    /// <summary>
    /// Renders form controls for enum choices from a label dictionary (see AOB.BAL.Common.Constants.ProjectOptions),
    /// so every screen builds its checkboxes, radios, dropdowns and multi-select dropdowns the same way. The posted value is the enum name.
    /// </summary>
    public static class EnumHtmlExtensions
    {
        /// <summary>
        /// A multi-select dropdown: a button showing "N selected" that opens a list of tick boxes, with the
        /// ticked choices shown as removable chips underneath. It posts exactly like a group of checkboxes
        /// (one value per ticked item, all under <paramref name="name"/>). The behaviour is in wwwroot/js/site.js.
        /// </summary>
        public static IHtmlContent MultiSelect(
            this IHtmlHelper html,
            string name,
            IEnumerable<MultiSelectItem> items,
            string placeholder = "Select…")
        {
            var list = items.ToList();
            var selected = list.Where(i => i.Selected && !i.Disabled).ToList();
            var sb = new StringBuilder();

            // Classes (ms-*) are styled in wwwroot/css/projects.css.
            sb.Append($"<div class=\"multi-select\" data-multi-select data-placeholder=\"{Enc(placeholder)}\">")
              .Append($"<button type=\"button\" class=\"ms-toggle{(selected.Count == 0 ? " is-empty" : "")}\" ")
              .Append("aria-haspopup=\"true\" aria-expanded=\"false\" data-multi-select-label>")
              .Append(Enc(selected.Count == 0 ? placeholder : $"{selected.Count} selected"))
              .Append("</button>")
              .Append("<div class=\"ms-menu\" hidden>");

            foreach (var item in list)
            {
                var id = $"{name}_{item.Value}";
                sb.Append($"<label class=\"ms-option{(item.Disabled ? " is-disabled" : "")}\" for=\"{Enc(id)}\" data-text=\"{Enc(item.Label)}\">")
                  .Append($"<input type=\"checkbox\" name=\"{Enc(name)}\" id=\"{Enc(id)}\" value=\"{Enc(item.Value)}\"")
                  .Append(item.Selected && !item.Disabled ? " checked" : "")
                  .Append(item.Disabled ? " disabled" : "")
                  .Append(" />")
                  .Append("<span class=\"ms-text\">")
                  .Append(Enc(item.Label));
                if (item.Badge is not null)
                {
                    sb.Append($" <span class=\"badge badge-gray\"><span class=\"bd\"></span>{Enc(item.Badge)}</span>");
                }
                if (!string.IsNullOrWhiteSpace(item.Hint))
                {
                    sb.Append($"<span class=\"ms-hint\">{Enc(item.Hint)}</span>");
                }
                sb.Append("</span></label>");
            }

            sb.Append("</div>")
              // Chips for the ticked choices (the script keeps them in step with the tick boxes).
              .Append("<div class=\"ms-chips\" data-multi-select-chips>");
            foreach (var item in selected)
            {
                sb.Append($"<span class=\"ms-chip\">{Enc(item.Label)}</span>");
            }
            sb.Append("</div></div>");

            return new HtmlString(sb.ToString());
        }

        /// <summary>
        /// A multi-select dropdown for an enum, labelled from a dictionary. Disabled values are listed but
        /// cannot be ticked (e.g. services that are "Coming soon").
        /// </summary>
        public static IHtmlContent EnumMultiSelect<TEnum>(
            this IHtmlHelper html,
            string name,
            IReadOnlyDictionary<TEnum, string> labels,
            IEnumerable<TEnum>? selected,
            IEnumerable<TEnum>? disabled = null,
            string? disabledBadge = null,
            string placeholder = "Select…") where TEnum : struct, Enum
        {
            var selectedSet = new HashSet<TEnum>(selected ?? Enumerable.Empty<TEnum>());
            var disabledSet = new HashSet<TEnum>(disabled ?? Enumerable.Empty<TEnum>());

            return html.MultiSelect(
                name,
                labels.Select(kv => new MultiSelectItem(
                    kv.Key.ToString(),
                    kv.Value,
                    Selected: selectedSet.Contains(kv.Key),
                    Disabled: disabledSet.Contains(kv.Key),
                    Badge: disabledSet.Contains(kv.Key) ? disabledBadge : null)),
                placeholder);
        }

        /// <summary>
        /// Pill-style multi-select: every option is visible; ticked options show as filled chips with a checkmark.
        /// Posts a list of values under one name.
        /// </summary>
        public static IHtmlContent EnumChipMultiSelect<TEnum>(
            this IHtmlHelper html,
            string name,
            IReadOnlyDictionary<TEnum, string> labels,
            IEnumerable<TEnum>? selected,
            IEnumerable<TEnum>? disabled = null) where TEnum : struct, Enum
        {
            var selectedSet = new HashSet<TEnum>(selected ?? Enumerable.Empty<TEnum>());
            var disabledSet = new HashSet<TEnum>(disabled ?? Enumerable.Empty<TEnum>());
            var sb = new StringBuilder();

            sb.Append("<div class=\"chip-select\" role=\"group\">");

            foreach (var (value, label) in labels)
            {
                var id = $"{name}_{value}";
                var isDisabled = disabledSet.Contains(value);
                var isSelected = selectedSet.Contains(value);
                sb.Append($"<label class=\"chip-option{(isDisabled ? " is-disabled" : "")}\" for=\"{Enc(id)}\">")
                  .Append($"<input type=\"checkbox\" name=\"{Enc(name)}\" id=\"{Enc(id)}\" value=\"{value}\"")
                  .Append(isSelected ? " checked" : "")
                  .Append(isDisabled ? " disabled" : "")
                  .Append(" />")
                  .Append("<span class=\"chip-face\">")
                  .Append("<span class=\"chip-check\" aria-hidden=\"true\">✓</span>")
                  .Append($"<span class=\"chip-label\">{Enc(label)}</span>")
                  .Append("</span></label>");
            }

            sb.Append("</div>");
            return new HtmlString(sb.ToString());
        }

        /// <summary>A group of Bootstrap radio buttons (posts one value).</summary>
        public static IHtmlContent EnumRadios<TEnum>(
            this IHtmlHelper html,
            string name,
            IReadOnlyDictionary<TEnum, string> labels,
            TEnum? selected) where TEnum : struct, Enum
        {
            var sb = new StringBuilder();

            foreach (var (value, label) in labels)
            {
                var id = $"{name}_{value}";
                sb.Append("<div class=\"form-check\">")
                  .Append($"<input class=\"form-check-input\" type=\"radio\" name=\"{Enc(name)}\" id=\"{Enc(id)}\" value=\"{value}\"")
                  .Append(selected.HasValue && selected.Value.Equals(value) ? " checked" : "")
                  .Append(" />")
                  .Append($"<label class=\"form-check-label\" for=\"{Enc(id)}\">{Enc(label)}</label></div>");
            }

            return new HtmlString(sb.ToString());
        }

        /// <summary>Items for a dropdown (use with asp-items); the value is the enum name.</summary>
        public static IEnumerable<SelectListItem> EnumSelectItems<TEnum>(
            this IHtmlHelper html,
            IReadOnlyDictionary<TEnum, string> labels) where TEnum : struct, Enum
            => labels.Select(kv => new SelectListItem(kv.Value, kv.Key.ToString())).ToList();

        /// <summary>Large selectable cards (checkboxes), e.g. PCI Pal services or payment methods.</summary>
        public static IHtmlContent EnumCardCheckboxes<TEnum>(
            this IHtmlHelper html,
            string name,
            IReadOnlyDictionary<TEnum, string> labels,
            IEnumerable<TEnum>? selected,
            IReadOnlyDictionary<TEnum, (string Hint, string Icon)>? meta = null,
            IEnumerable<TEnum>? disabled = null,
            string disabledBadge = "Coming soon",
            string layout = "stack") where TEnum : struct, Enum
        {
            var selectedSet = new HashSet<TEnum>(selected ?? Enumerable.Empty<TEnum>());
            var disabledSet = new HashSet<TEnum>(disabled ?? Enumerable.Empty<TEnum>());
            var sb = new StringBuilder();
            var layoutClass = layout switch
            {
                "grid-2" => " option-cards--cols-2 option-cards--check-tr",
                "grid-3" => " option-cards--cols-3",
                "grid-3-check-tr" => " option-cards--cols-3 option-cards--check-tr",
                "grid-3-proto" => " option-cards--cols-3 option-cards--proto",
                "grid-auto" => " option-cards--auto option-cards--check-tr",
                "grid-auto-proto" => " option-cards--auto option-cards--proto",
                _ => ""
            };
            sb.Append($"<div class=\"option-cards{layoutClass}\" role=\"group\">");

            foreach (var (value, label) in labels)
            {
                var id = $"{name}_{value}";
                var isDisabled = disabledSet.Contains(value);
                (string Hint, string Icon) m = default;
                if (meta is not null)
                    meta.TryGetValue(value, out m);
                sb.Append($"<label class=\"option-card{(isDisabled ? " is-disabled" : "")}\" for=\"{Enc(id)}\">")
                  .Append($"<input type=\"checkbox\" name=\"{Enc(name)}\" id=\"{Enc(id)}\" value=\"{value}\"")
                  .Append(selectedSet.Contains(value) ? " checked" : "")
                  .Append(isDisabled ? " disabled" : "")
                  .Append(" />")
                  .Append("<span class=\"option-card-body\">")
                  .Append("<span class=\"option-card-check\" aria-hidden=\"true\"></span>")
                  .Append("<span class=\"option-card-text\">")
                  .Append("<span class=\"option-card-title\">").Append(Enc(label));
                if (isDisabled)
                {
                    sb.Append($" <span class=\"tag soon\">{Enc(disabledBadge)}</span>");
                }
                sb.Append("</span>");
                if (m.Hint is { Length: > 0 } hint)
                {
                    sb.Append($"<span class=\"option-card-hint\">{Enc(hint)}</span>");
                }
                sb.Append("</span>");
                if (!string.IsNullOrWhiteSpace(m.Icon))
                {
                    sb.Append("<span class=\"option-card-icon\" aria-hidden=\"true\">")
                      .Append(FormIcons.Render(m.Icon))
                      .Append("</span>");
                }
                sb.Append("</span></label>");
            }

            sb.Append("</div>");
            return new HtmlString(sb.ToString());
        }

        /// <summary>Large selectable cards (radio buttons), e.g. CRM payment experience or card masking.</summary>
        public static IHtmlContent EnumCardRadios<TEnum>(
            this IHtmlHelper html,
            string name,
            IReadOnlyDictionary<TEnum, string> labels,
            TEnum? selected,
            IReadOnlyDictionary<TEnum, string>? hints = null,
            IReadOnlyDictionary<TEnum, string>? previews = null,
            IReadOnlyDictionary<TEnum, (string Hint, string Icon)>? cardMeta = null,
            string layout = "stack") where TEnum : struct, Enum
        {
            var sb = new StringBuilder();
            var layoutClass = layout switch
            {
                "grid-2" => " option-cards--cols-2 option-cards--check-tr",
                "grid-2-check-tr" => " option-cards--cols-2 option-cards--check-tr",
                "grid-2-proto" => " option-cards--cols-2 option-cards--proto",
                "grid-3-proto" => " option-cards--cols-3 option-cards--proto",
                "grid-3" => " option-cards--cols-3",
                _ => ""
            };
            sb.Append($"<div class=\"option-cards{layoutClass}\" role=\"radiogroup\">");

            foreach (var (value, label) in labels)
            {
                var id = $"{name}_{value}";
                string? hint = null;
                string? preview = null;
                string? icon = null;
                if (cardMeta is not null && cardMeta.TryGetValue(value, out var meta))
                {
                    hint = meta.Hint;
                    icon = meta.Icon;
                }
                else
                {
                    hints?.TryGetValue(value, out hint);
                }
                previews?.TryGetValue(value, out preview);
                sb.Append($"<label class=\"option-card\" for=\"{Enc(id)}\">")
                  .Append($"<input type=\"radio\" name=\"{Enc(name)}\" id=\"{Enc(id)}\" value=\"{value}\"")
                  .Append(selected.HasValue && selected.Value.Equals(value) ? " checked" : "")
                  .Append(" />")
                  .Append("<span class=\"option-card-body\">")
                  .Append("<span class=\"option-card-check option-card-check--radio\" aria-hidden=\"true\"></span>")
                  .Append("<span class=\"option-card-text\">")
                  .Append($"<span class=\"option-card-title\">{Enc(label)}</span>");
                if (!string.IsNullOrWhiteSpace(hint))
                {
                    sb.Append($"<span class=\"option-card-hint\">{Enc(hint)}</span>");
                }
                if (!string.IsNullOrWhiteSpace(preview))
                {
                    sb.Append($"<span class=\"option-card-preview\">{Enc(preview)}</span>");
                }
                sb.Append("</span>");
                if (!string.IsNullOrWhiteSpace(icon))
                {
                    sb.Append("<span class=\"option-card-icon\" aria-hidden=\"true\">")
                      .Append(FormIcons.Render(icon))
                      .Append("</span>");
                }
                sb.Append("</span></label>");
            }

            sb.Append("</div>");
            return new HtmlString(sb.ToString());
        }

        private static string Enc(string value) => WebUtility.HtmlEncode(value);
    }

    internal static class FormIcons
    {
        public static string Render(string? icon) => icon switch
        {
            "link" => "<svg class=\"ic\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\"><path d=\"M10 13a5 5 0 007.1 0l2-2a5 5 0 00-7.1-7.1l-1.2 1.2M14 11a5 5 0 00-7.1 0l-2 2a5 5 0 007.1 7.1l1.2-1.2\"/></svg>",
            "phone" => "<svg class=\"ic\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\"><path d=\"M22 16.9v3a2 2 0 01-2.2 2 19.8 19.8 0 01-8.6-3.1 19.5 19.5 0 01-6-6A19.8 19.8 0 012.1 4.2 2 2 0 014.1 2h3a2 2 0 012 1.7c.1.9.3 1.8.6 2.6a2 2 0 01-.5 2.1L8 9.9a16 16 0 006 6l1.5-1.2a2 2 0 012.1-.5c.8.3 1.7.5 2.6.6A2 2 0 0122 16.9z\"/></svg>",
            "keypad" => "<svg class=\"ic\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\"><rect x=\"4\" y=\"4\" width=\"16\" height=\"16\" rx=\"2\"/><path d=\"M8 8h.01M12 8h.01M16 8h.01M8 12h.01M12 12h.01M16 12h.01M8 16h8\"/></svg>",
            "card" => "<svg class=\"ic\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\"><rect x=\"2\" y=\"5\" width=\"20\" height=\"14\" rx=\"2\"/><path d=\"M2 10h20\"/></svg>",
            "help" => "<svg class=\"ic\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\"><circle cx=\"12\" cy=\"12\" r=\"9\"/><path d=\"M9.5 9a2.5 2.5 0 115 0c0 2-2.5 2-2.5 4M12 17h.01\"/></svg>",
            "layout" => "<svg class=\"ic\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\"><rect x=\"3\" y=\"3\" width=\"18\" height=\"18\" rx=\"2\"/><path d=\"M3 9h18M9 21V9\"/></svg>",
            "external" => "<svg class=\"ic\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\"><path d=\"M18 13v6a2 2 0 01-2 2H5a2 2 0 01-2-2V8a2 2 0 012-2h6M15 3h6v6M10 14L21 3\"/></svg>",
            _ => "<svg class=\"ic\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\"><circle cx=\"12\" cy=\"12\" r=\"9\"/></svg>"
        };
    }
}
