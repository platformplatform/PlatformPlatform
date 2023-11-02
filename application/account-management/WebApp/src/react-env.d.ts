// CSS
declare module "*.css" {
  /**
   * @deprecated Use `import style from './main.css?inline'` instead.
   */
  const css: string;
  export default css;
}

// images
declare module "*.png" {
  const src: string;
  export default src;
}
declare module "*.jpg" {
  const src: string;
  export default src;
}
declare module "*.svg" {
  const ReactComponent: React.FC<React.SVGProps<SVGSVGElement>>;
  const content: string;

  export { ReactComponent };
  export default content;
}
declare module "*.ico" {
  const src: string;
  export default src;
}
declare module "*.webp" {
  const src: string;
  export default src;
}

// media
declare module "*.webm" {
  const src: string;
  export default src;
}
