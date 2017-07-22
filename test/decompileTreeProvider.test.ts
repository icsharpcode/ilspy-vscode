import { expect, should } from 'chai';
import { MemberNode } from '../src/msildecompiler/decompiledTreeProvider';
import { TokenType } from '../src/msildecompiler/tokenType';

suite("Member Node Tests", () => {
    test("Assembly node returns true for isType", () => {
        const node = new MemberNode("name", 0, TokenType.Assembly, -1);

        expect(node.isType).to.be.a('boolean');
        expect(node.isType).to.equal(true);
    });

    test("Type node returns true for isType", () => {
        const node = new MemberNode("name", 0, TokenType.TypeDef, 0);

        expect(node.isType).to.be.a('boolean');
        expect(node.isType).to.equal(true);
    });


    test("Other nodes return false for isType", () => {
        const node = new MemberNode("name", 0, TokenType.MemberRef, 0);

        expect(node.isType).to.be.a('boolean');
        expect(node.isType).to.equal(false);
    });
});

suite("Tree Data Provider tests", () => {

});