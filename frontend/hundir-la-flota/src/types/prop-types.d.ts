export {};

declare module 'prop-types';
declare module "signalr";
declare global {
  interface Window {
    socket: any;
  }
}