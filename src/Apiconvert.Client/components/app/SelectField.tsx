"use client";

import { useState } from "react";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { cn } from "@/lib/utils";

type SelectOption = {
  value: string;
  label: string;
  disabled?: boolean;
  formValue?: string;
};

export function SelectField({
  id,
  name,
  options,
  defaultValue,
  value,
  onValueChange,
  placeholder,
  disabled,
  size = "default",
  triggerClassName,
}: {
  id?: string;
  name: string;
  options: SelectOption[];
  defaultValue?: string;
  value?: string;
  onValueChange?: (value: string) => void;
  placeholder?: string;
  disabled?: boolean;
  size?: "sm" | "default";
  triggerClassName?: string;
}) {
  const isControlled = value !== undefined;
  const [internalValue, setInternalValue] = useState(defaultValue ?? "");
  const currentValue = isControlled ? value : internalValue;
  const selectedOption = options.find((option) => option.value === currentValue);
  const formValue = selectedOption?.formValue ?? currentValue ?? "";

  const handleChange = (nextValue: string) => {
    if (!isControlled) {
      setInternalValue(nextValue);
    }
    onValueChange?.(nextValue);
  };

  return (
    <>
      <Select
        value={currentValue || undefined}
        onValueChange={handleChange}
        disabled={disabled}
      >
        <SelectTrigger
          id={id}
          size={size}
          className={cn("w-full", triggerClassName)}
        >
          <SelectValue placeholder={placeholder} />
        </SelectTrigger>
        <SelectContent>
          {options.map((option) => (
            <SelectItem
              key={option.value}
              value={option.value}
              disabled={option.disabled}
            >
              {option.label}
            </SelectItem>
          ))}
        </SelectContent>
      </Select>
      <input type="hidden" name={name} value={formValue} />
    </>
  );
}
