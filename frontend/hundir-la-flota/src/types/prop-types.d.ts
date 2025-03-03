export {};

declare module 'prop-types';
declare module "signalr";
declare global {
  interface Window {
    socket: unknown;
  }

  interface ButtonProps {
    children?: React.ReactNode;
    label?: string;
    onClick?: () => void;
    type?: 'button' | 'submit' | 'reset';
    className?: string;
    loading?: boolean;
  }
  
}