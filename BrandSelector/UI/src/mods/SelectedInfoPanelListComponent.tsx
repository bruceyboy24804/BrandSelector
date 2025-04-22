import { getModule } from "cs2/modding";
import { Theme } from "cs2/bindings";
import { bindValue, useValue } from "cs2/api";
import { useLocalization } from "cs2/l10n";
import mod from "../../mod.json";
import { VanillaComponentResolver } from "mods/VanillaComponentResolver/VanillaComponentResolver";
interface InfoSectionComponent {
	group: string;
	tooltipKeys: Array<string>;
	tooltipTags: Array<string>;
}
interface BrandList{
    id: string;
    name: string;
}
const InfoRowTheme: Theme | any = getModule(
	"game-ui/game/components/selected-info-panel/shared-components/info-row/info-row.module.scss",
	"classes"
)

const InfoSection: any = getModule( 
    "game-ui/game/components/selected-info-panel/shared-components/info-section/info-section.tsx",
    "InfoSection"
)

const InfoRow: any = getModule(
    "game-ui/game/components/selected-info-panel/shared-components/info-row/info-row.tsx",
    "InfoRow"
)

const descriptionToolTipStyle = getModule("game-ui/common/tooltip/description-tooltip/description-tooltip.module.scss", "classes");

export const SelectedInfoPanelListComponent = (componentList: any): any => {
  // Add BrandListSection component
  componentList["BrandSelector.Systems.BrandListSection"] = (e: InfoSectionComponent) => {
    const brands$ = bindValue<BrandList[]>(mod.id,'brands');
    const brands = useValue(brands$);
    const { translate } = useLocalization();

    const brandSectionTitle = translate("BrandSelector.SECTION_TITLE", "Brands");

    return (
      <InfoSection focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED} disableFocus={true}>
        <InfoRow
          left={brandSectionTitle}
          uppercase={true}
          disableFocus={true}
          subRow={false}
          className={InfoRowTheme.infoRow}
        />
        <InfoRow
          disableFocus={true}
          subRow={true}
          className={InfoRowTheme.infoRow}
          right={
            <ul style={{ listStyleType: "disc", paddingLeft: "16px", margin: "4px 0" }}>
              {brands && brands.map((brand, index) => (
                <li key={index}>{brand.name}</li>
              ))}
            </ul>
          }
        />
      </InfoSection>
    );
  };

  return componentList as any;
};