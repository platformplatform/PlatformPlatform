"use client";

import { MoonStarIcon, SunIcon, SunMoonIcon } from "lucide-react";
import { useState } from "react";
import { Button } from "../components/Button";
import { DialogTrigger, Selection } from "react-aria-components";
import { Popover } from "../components/Popover";
import { Dialog } from "../components/Dialog";
import { ListBox, ListBoxItem } from "../components/ListBox";
import { useThemeMode } from "./ThemeContext";
import { ThemeMode } from "./themeModeCookie";

export function ThemeModeSwitcher() {
  const [open, setOpen] = useState(false);
  const { themeMode, setThemeMode } = useThemeMode();

  function handleChange(keys: Selection) {
    if (keys instanceof Set) {
      const mode = keys.keys().next().value as ThemeMode;
      setOpen(false);
      setThemeMode(mode);
    }
  }
  return (
    <DialogTrigger isOpen={open} onOpenChange={setOpen}>
      <Button variant="icon">
        {themeMode === "light" ? (
          <SunIcon />
        ) : themeMode === "dark" ? (
          <MoonStarIcon />
        ) : (
          <SunMoonIcon />
        )}
      </Button>
      <Popover showArrow isOpen={open} onOpenChange={setOpen}>
        <Dialog className="w-52">
          <ListBox
            className="border-none"
            aria-label="Theme mode"
            selectionMode="single"
            selectedKeys={[themeMode]}
            onSelectionChange={handleChange}
          >
            <ListBoxItem id="light" textValue="light">
              <SunIcon /> Light
            </ListBoxItem>
            <ListBoxItem id="dark" textValue="dark">
              <MoonStarIcon /> Dark
            </ListBoxItem>
            <ListBoxItem id="system" textValue="system">
              <SunMoonIcon /> System
            </ListBoxItem>
          </ListBox>
        </Dialog>
      </Popover>
    </DialogTrigger>
  );
}
