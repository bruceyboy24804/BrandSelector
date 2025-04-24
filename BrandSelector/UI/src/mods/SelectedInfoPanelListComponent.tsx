import { getModule } from "cs2/modding";
import { Theme } from "cs2/bindings";
import { useValue, trigger } from "cs2/api";
import {LocalizedEntityName, useLocalization} from "cs2/l10n";
import { VanillaComponentResolver } from "mods/VanillaComponentResolver/VanillaComponentResolver";
import { selectedBrand$, availableBrands$, BrandInfo } from "./bindings";
import {Dropdown,
    DropdownItem,
    DropdownToggle,} from "cs2/ui"
import mod from "mod.json"
import styles from "./SelectedInfoPanelListComponent.module.scss";
import {Entity, Name, NameType} from "cs2/bindings";
import React from "react";

interface InfoSectionComponent {
	group: string;
	tooltipKeys: Array<string>;
	tooltipTags: Array<string>;
}
const DropdownStyle: Theme | any = getModule("game-ui/menu/themes/dropdown.module.scss", "classes");
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
  componentList["BrandSelector.Systems.BrandListSection"] = (e: InfoSectionComponent) => {
    const selectedBrand = useValue(selectedBrand$);
    const availableBrands = useValue(availableBrands$);
    const { translate } = useLocalization();

    const brandSectionTitle = translate("BrandSelector.SECTION_TITLE", "Brands");
    
    // Create dropdown items for each available brand
    const brandOptions = availableBrands.map(brandInfo => {
      // Direct comparison is now possible using the name
      const isSelected = selectedBrand && brandInfo.name === selectedBrand.name;
      
      return (
        <DropdownItem
          theme={DropdownStyle}
          value={brandInfo}
          closeOnSelect={true}
          onChange={() => trigger(mod.id, "selectBrand", brandInfo)}
          className={isSelected ? styles.selectedDropdownItem : ""}
        >
          <div className={styles.dropdownName}>{brandInfo.name}</div>
        </DropdownItem>
      );
    });

    // Function to render brand name
    const renderSelectedBrand = () => {
      if (!selectedBrand || !selectedBrand.entity) {
        return translate("BrandSelector.SELECT_BRAND", "Select Brand");
      }
      
      // Simply display the name we received from C# code
      return selectedBrand.name;
    };

    return (
      <InfoSection focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED} disableFocus={true}>
        <InfoRow
          left={brandSectionTitle}
          uppercase={true}
          disableFocus={true}
          subRow={false}
          className={InfoRowTheme.infoRow}
          right={
            availableBrands.length > 0 && (
              <Dropdown
                theme={DropdownStyle}
                content={brandOptions}
              >
                {renderSelectedBrand()}
              </Dropdown>
            )
          }
        />
      </InfoSection>
    );
  };

  return componentList as any;
};

