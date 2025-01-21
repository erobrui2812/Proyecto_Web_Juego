export {};

declare module 'prop-types';
declare module "signalr";
declare global {
  interface Window {
    socket: any;
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