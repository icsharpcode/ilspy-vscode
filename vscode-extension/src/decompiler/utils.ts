import { MemberNode } from "./MemberNode";

// metadata tokens/handles are 32-bit unsigned integers in the format:
// the first byte is the handle kind/token type, the other three bytes are used for the row-id.
export function makeHandle(element: MemberNode): number {
  return (element.type << 24) | element.rid;
}
