import type { ReactNode } from 'react';

export type ButtonVariant = 'primary' | 'secondary' | 'icon' | 'ghost';
export type ButtonSize = 'sm' | 'md' | 'lg';

export interface ButtonProps {
  variant?: ButtonVariant;
  size?: ButtonSize;
  onClick?: () => void;
  disabled?: boolean;
  children?: ReactNode;
  icon?: ReactNode;
  className?: string;
}

export function Button({
  variant = 'primary',
  size = 'md',
  onClick,
  disabled = false,
  children,
  icon,
  className = '',
}: ButtonProps) {
  const baseClass = 'btn';
  const variantClass = `btn--${variant}`;
  const sizeClass = `btn--${size}`;

  return (
    <button
      type="button"
      className={`${baseClass} ${variantClass} ${sizeClass} ${className}`}
      onClick={onClick}
      disabled={disabled}
    >
      {icon && <span className="btn__icon">{icon}</span>}
      {children && <span className="btn__label">{children}</span>}
    </button>
  );
}
