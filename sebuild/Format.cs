using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using static Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions;
using static Microsoft.CodeAnalysis.Formatting.FormattingOptions;

namespace SeBuild;

public static class FormatterOpts {
    public static OptionSet Apply(OptionSet o) {
        return o
            .WithChangedOption(IndentBlock, false)
            .WithChangedOption(IndentBraces, false)
            .WithChangedOption(IndentSwitchSection, false)
            .WithChangedOption(IndentSwitchCaseSectionWhenBlock, false)
            .WithChangedOption(IndentSwitchCaseSection, false)
            .WithChangedOption(LabelPositioning, LabelPositionOptions.NoIndent)
            .WithChangedOption(NewLineForCatch, false)
            .WithChangedOption(NewLineForClausesInQuery, false)
            .WithChangedOption(NewLineForElse, false)
            .WithChangedOption(NewLine, LanguageNames.CSharp, "")
            .WithChangedOption(TabSize, LanguageNames.CSharp, 0);
    }
}
