import { ModRegistrar } from "cs2/modding";
import { SelectedInfoPanelListComponent } from "./mods/SelectedInfoPanelListComponent";
import { VanillaComponentResolver } from "mods/VanillaComponentResolver/VanillaComponentResolver";

const register: ModRegistrar = (moduleRegistry) => {
     VanillaComponentResolver.setRegistry(moduleRegistry);

         moduleRegistry.extend("game-ui/game/components/selected-info-panel/selected-info-sections/selected-info-sections.tsx", 'selectedInfoSectionComponents', SelectedInfoPanelListComponent);

}

export default register;