using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using static Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions;
using static Microsoft.CodeAnalysis.Formatting.FormattingOptions;

namespace SeBuild;

public static class FormatterOpts {
    public static OptionSet Apply(DocumentOptionSet o) {
        return o
            .WithChangedOption(NewLine, "\n")
            .WithChangedOption(TabSize, 20)
            .WithChangedOption(SmartIndent, IndentStyle.None)
            .WithChangedOption(UseTabs, false)
            .WithChangedOption(IndentSwitchSection, false)
            .WithChangedOption(NewLinesForBracesInMethods, false)
            .WithChangedOption(NewLinesForBracesInTypes, false)
            .WithChangedOption(NewLinesForBracesInControlBlocks, false)
            .WithChangedOption(IndentBlock, false)
            .WithChangedOption(IndentBraces, false)
            .WithChangedOption(IndentSwitchSection, false)
            .WithChangedOption(IndentSwitchCaseSectionWhenBlock, false)
            .WithChangedOption(IndentSwitchCaseSection, false)
            .WithChangedOption(LabelPositioning, LabelPositionOptions.NoIndent)
            .WithChangedOption(NewLineForCatch, false)
            .WithChangedOption(NewLineForClausesInQuery, false)
            .WithChangedOption(NewLineForElse, false);

    }
}
