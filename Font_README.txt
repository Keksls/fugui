Fugui font documentation

Fugui handle Font and Icons merging.
It means you can add icons to your fonts to display icons on UI.
Icons must be in separated .tff / .otf.
You can use Duotone icons. If so, you need to arange your glyph like : PrimaryGlyph => Secondary glyph right next of each others.
For exemple, you want a duotone icon that draw two square, one of primary color on the other of secondary color,
you make the primary glyph on glyph EC7F, you will need to make the secondary glyph on EC80.

Please note that you need to use right glyph ranges : 

Fugui reserved glyph ranges (you must NOT use thes ranges => ranges are included) :
	- Regular Icons : 57344 -> 57444 (E000 -> E064)
	- Duotone Icons : 60543 -> 60643 (EC7F -> ECE3)


RANGES YOU MUST USE FOR YOUR ICONS :
	- Regular Icons : 57445 -> 60542 (E065 -> EC7E)
	- Duotone Icons : 60644 -> 63743 (ECE4 -> F8FF)

DUOTONE Icons Color :
	Primary color is Text color
	Secondary color is DisabledText color