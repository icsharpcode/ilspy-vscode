A back end service to provide MSIL decompilations

Supported commands are of the following format

```javascript
{"Seq":1,"Command":"/assembly"}
{"Seq":2,"Command":"/types"}
{"Seq":3,"Command":"/type",Arguments:{"Rid":2}}
{"Seq":4,"Command":"/members",Arguments:{"Rid":2}}
{"Seq":4,"Command":"/member",Arguments:{"TypeRid":2,"MemberType":67108864,"MemberRid":1}}
```