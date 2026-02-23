import { h } from "vue";
import DefaultTheme from "vitepress/theme";
import RuntimeSelector from "./runtime-selector.vue";
import "./custom.css";

export default {
  ...DefaultTheme,
  Layout: () => {
    return h(DefaultTheme.Layout, null, {
      "nav-bar-content-after": () => h(RuntimeSelector)
    });
  }
};
