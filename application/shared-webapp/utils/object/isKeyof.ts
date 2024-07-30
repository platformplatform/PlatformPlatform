export function isKeyof<O extends {}>(key: string | symbol | keyof O, object: O): key is keyof O {
  return typeof key === "string" && key in object;
}
