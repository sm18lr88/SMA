# SuperMemo Assistant Formulation plugin

The formulation advisor checks SuperMemo items against the [20 rules of formulating knowledge](https://supermemo.guru/wiki/20_rules_of_knowledge_formulation) by Piotr Wozniak. It tells you how you can improve an item. You decide what to change.

This plugin is part of [SuperMemoAssistant 20 Community](../../../README.md), which runs on SuperMemo 20 (64-bit) and .NET 10. It is bundled with the installer.

## How to use it

- Press **Ctrl+Alt+Shift+F** in SuperMemo to check the current item. A small window lists the findings, each with an explanation and a link to the rule. If no rule applies, the window says so.
- The automatic check runs each time SuperMemo shows an item. It shows a notification only when a warning applies. It shows at most one notification for each element in a session.
- The notification does not quote the item, so it does not show the answer during a repetition. Press the hotkey after you grade the item to read the findings.

The advisor only reads the collection. It never changes an element, and it never opens a modal dialog. It checks items only, not topics. It uses no network and no AI service.

## Rules

| Rule | The advisor reports | Severity |
| --- | --- | --- |
| 4. Minimum information principle | An answer or cloze deletion with more than 10 words. | Warning |
| 5. Cloze deletion is easy and effective | More than one cloze placeholder "[...]" in one item. | Warning |
| 9. Avoid sets | An answer with 3 or more parts: an HTML list, or parts separated by commas, semicolons, or "and". | Warning |
| 10. Avoid enumerations | A question that asks for a list, for example "List", "Name all", or "What are the". | Advice |
| 13. Context cues simplify wording | A cloze sentence that starts with a pronoun such as "He" or "This". | Advice |
| 15. Optimize wording | A question with more than 30 words. | Advice |
| 18. Provide sources | An item that has no Source and no Link reference. | Advice |
| 19. Provide date stamping | A word such as "currently" or "latest" when the item has no year and no Date reference. | Advice |

Each finding quotes the text that caused it. Each finding links to the matching section of the [original 20 rules article](https://super-memory.com/articles/20rules.htm). The summary page on supermemo.guru has no section for each rule.

## Settings

Open the plugin settings in SMA to change these values:

- Turn the automatic check on or off. It is on by default.
- Turn each rule on or off.
- Set the maximum number of words in an answer or cloze deletion. The default is 10.
- Set the maximum number of words in a question. The default is 30.
- Set the minimum number of parts in a set. The default is 3.

You can change the hotkey in the SMA hotkey settings.

## How the advisor reads an item

SMA reads the element that the element window shows. The advisor reads the components of the element from the component registry. Each HTML or text component points to a member of the text registry, which holds its content.

The "display at" flags of a component tell where it belongs. A component that shows at the question is part of the question. A component that is hidden at the question but shows at the answer is part of the answer.

The advisor removes tags and entities before it checks the text. SuperMemo keeps references in a block at the end of the HTML. The advisor reads Title, Source, Link, and Date from that block.

## Known limits

- The advisor reads the saved content of the item. It does not see changes that SuperMemo has not saved yet.
- The advisor cannot read RTF components, so it skips them.
- The rules use simple text patterns. They can miss a problem, and they can report a problem that is not there.

## Extend the rule set

Each rule is one class that implements `IFormulationRule`. Add the class to `FormulationAnalyzer.CreateDefaultRules`, and add a switch to `FormulationCfg`. The rule engine has no SuperMemo or WPF dependency, so unit tests can cover each rule.

## License

MIT. See [LICENSE](LICENSE).
