"use strict";(self.webpackChunkjitterphysics=self.webpackChunkjitterphysics||[]).push([[529],{9754:(e,s,a)=>{a.r(s),a.d(s,{assets:()=>c,contentTitle:()=>r,default:()=>o,frontMatter:()=>t,metadata:()=>l,toc:()=>m});var n=a(4848),i=a(8453);const t={},r="Dynamic tree",l={id:"documentation/dynamictree",title:"Dynamic tree",description:"The dynamic tree in Jitter holds instances which implement the IDynamicTreeProxy interface.",source:"@site/docs/02_documentation/05-dynamictree.md",sourceDirName:"02_documentation",slug:"/documentation/dynamictree",permalink:"/docs/documentation/dynamictree",draft:!1,unlisted:!1,editUrl:"https://github.com/notgiven688/jitterphysics2/tree/main/docs/docs/02_documentation/05-dynamictree.md",tags:[],version:"current",sidebarPosition:5,frontMatter:{},sidebar:"tutorialSidebar",previous:{title:"Constraints",permalink:"/docs/documentation/constraints"},next:{title:"Collision Filters",permalink:"/docs/documentation/filters"}},c={},m=[{value:"Adding proxies",id:"adding-proxies",level:2},{value:"Potential pairs",id:"potential-pairs",level:2},{value:"Querying the tree",id:"querying-the-tree",level:2},{value:"Ray casting",id:"ray-casting",level:2}];function h(e){const s={annotation:"annotation",code:"code",h1:"h1",h2:"h2",header:"header",img:"img",math:"math",mi:"mi",mn:"mn",mo:"mo",mrow:"mrow",mspace:"mspace",mtext:"mtext",p:"p",pre:"pre",semantics:"semantics",span:"span",...(0,i.R)(),...e.components};return(0,n.jsxs)(n.Fragment,{children:[(0,n.jsx)(s.header,{children:(0,n.jsx)(s.h1,{id:"dynamic-tree",children:"Dynamic tree"})}),"\n",(0,n.jsxs)(s.p,{children:["The dynamic tree in Jitter holds instances which implement the ",(0,n.jsx)(s.code,{children:"IDynamicTreeProxy"})," interface.\nThe main task of the tree is to efficiently determine if a proxy\u2019s axis-aligned bounding box is overlapping with the axis-aligned bounding box of any other proxy in the world.\nIn a naive implementation this requires ",(0,n.jsxs)(s.span,{className:"katex",children:[(0,n.jsx)(s.span,{className:"katex-mathml",children:(0,n.jsx)(s.math,{xmlns:"http://www.w3.org/1998/Math/MathML",children:(0,n.jsxs)(s.semantics,{children:[(0,n.jsxs)(s.mrow,{children:[(0,n.jsx)(s.mi,{mathvariant:"script",children:"O"}),(0,n.jsxs)(s.mrow,{children:[(0,n.jsx)(s.mo,{fence:"true",children:"("}),(0,n.jsx)(s.mi,{children:"n"}),(0,n.jsx)(s.mo,{fence:"true",children:")"})]})]}),(0,n.jsx)(s.annotation,{encoding:"application/x-tex",children:"\\mathcal{O}\\left(n\\right)"})]})})}),(0,n.jsx)(s.span,{className:"katex-html","aria-hidden":"true",children:(0,n.jsxs)(s.span,{className:"base",children:[(0,n.jsx)(s.span,{className:"strut",style:{height:"1em",verticalAlign:"-0.25em"}}),(0,n.jsx)(s.span,{className:"mord mathcal",style:{marginRight:"0.02778em"},children:"O"}),(0,n.jsx)(s.span,{className:"mspace",style:{marginRight:"0.1667em"}}),(0,n.jsxs)(s.span,{className:"minner",children:[(0,n.jsx)(s.span,{className:"mopen delimcenter",style:{top:"0em"},children:"("}),(0,n.jsx)(s.span,{className:"mord mathnormal",children:"n"}),(0,n.jsx)(s.span,{className:"mclose delimcenter",style:{top:"0em"},children:")"})]})]})})]})," operations (checking for an overlap with every of the ",(0,n.jsxs)(s.span,{className:"katex",children:[(0,n.jsx)(s.span,{className:"katex-mathml",children:(0,n.jsx)(s.math,{xmlns:"http://www.w3.org/1998/Math/MathML",children:(0,n.jsxs)(s.semantics,{children:[(0,n.jsxs)(s.mrow,{children:[(0,n.jsx)(s.mi,{children:"n"}),(0,n.jsx)(s.mo,{children:"\u2212"}),(0,n.jsx)(s.mn,{children:"1"})]}),(0,n.jsx)(s.annotation,{encoding:"application/x-tex",children:"n-1"})]})})}),(0,n.jsxs)(s.span,{className:"katex-html","aria-hidden":"true",children:[(0,n.jsxs)(s.span,{className:"base",children:[(0,n.jsx)(s.span,{className:"strut",style:{height:"0.6667em",verticalAlign:"-0.0833em"}}),(0,n.jsx)(s.span,{className:"mord mathnormal",children:"n"}),(0,n.jsx)(s.span,{className:"mspace",style:{marginRight:"0.2222em"}}),(0,n.jsx)(s.span,{className:"mbin",children:"\u2212"}),(0,n.jsx)(s.span,{className:"mspace",style:{marginRight:"0.2222em"}})]}),(0,n.jsxs)(s.span,{className:"base",children:[(0,n.jsx)(s.span,{className:"strut",style:{height:"0.6444em"}}),(0,n.jsx)(s.span,{className:"mord",children:"1"})]})]})]})," entities).\nThe tree structure does accelerate this to ",(0,n.jsxs)(s.span,{className:"katex",children:[(0,n.jsx)(s.span,{className:"katex-mathml",children:(0,n.jsx)(s.math,{xmlns:"http://www.w3.org/1998/Math/MathML",children:(0,n.jsxs)(s.semantics,{children:[(0,n.jsxs)(s.mrow,{children:[(0,n.jsx)(s.mi,{mathvariant:"script",children:"O"}),(0,n.jsxs)(s.mrow,{children:[(0,n.jsx)(s.mo,{fence:"true",children:"("}),(0,n.jsxs)(s.mrow,{children:[(0,n.jsx)(s.mi,{mathvariant:"normal",children:"l"}),(0,n.jsx)(s.mi,{mathvariant:"normal",children:"o"}),(0,n.jsx)(s.mi,{mathvariant:"normal",children:"g"})]}),(0,n.jsx)(s.mo,{stretchy:"false",children:"("}),(0,n.jsx)(s.mi,{children:"n"}),(0,n.jsx)(s.mo,{stretchy:"false",children:")"}),(0,n.jsx)(s.mo,{fence:"true",children:")"})]})]}),(0,n.jsx)(s.annotation,{encoding:"application/x-tex",children:"\\mathcal{O}\\left(\\mathrm{log}(n)\\right)"})]})})}),(0,n.jsx)(s.span,{className:"katex-html","aria-hidden":"true",children:(0,n.jsxs)(s.span,{className:"base",children:[(0,n.jsx)(s.span,{className:"strut",style:{height:"1em",verticalAlign:"-0.25em"}}),(0,n.jsx)(s.span,{className:"mord mathcal",style:{marginRight:"0.02778em"},children:"O"}),(0,n.jsx)(s.span,{className:"mspace",style:{marginRight:"0.1667em"}}),(0,n.jsxs)(s.span,{className:"minner",children:[(0,n.jsx)(s.span,{className:"mopen delimcenter",style:{top:"0em"},children:"("}),(0,n.jsx)(s.span,{className:"mord",children:(0,n.jsx)(s.span,{className:"mord mathrm",style:{marginRight:"0.01389em"},children:"log"})}),(0,n.jsx)(s.span,{className:"mopen",children:"("}),(0,n.jsx)(s.span,{className:"mord mathnormal",children:"n"}),(0,n.jsx)(s.span,{className:"mclose",children:")"}),(0,n.jsx)(s.span,{className:"mclose delimcenter",style:{top:"0em"},children:")"})]})]})})]}),". Since proxies are dynamic and can move, the tree must be continuously updated.\nTo less frequently trigger updates, entities are enclosed within slightly larger bounding boxes than their actual size.\nThis bounding box extension is defined by the ",(0,n.jsx)(s.code,{children:"Velocity"})," property of the ",(0,n.jsx)(s.code,{children:"IDynamicTreeProxy"})," interface."]}),"\n",(0,n.jsx)(s.p,{children:(0,n.jsx)(s.img,{alt:"img alt",src:a(5133).A+"",width:"1058",height:"595"})}),"\n",(0,n.jsx)(s.h2,{id:"adding-proxies",children:"Adding proxies"}),"\n",(0,n.jsxs)(s.p,{children:["Jitter automatically registers all shapes added to a rigid body (",(0,n.jsx)(s.code,{children:"body.AddShape"}),") with the ",(0,n.jsx)(s.code,{children:"world.DynamicTree"}),".\nHowever, users are free to add own implementations of ",(0,n.jsx)(s.code,{children:"IDynamicTreeProxy"})," to the world's tree, using ",(0,n.jsx)(s.code,{children:"tree.AddProxy"}),".\nIn this case the user has to implement a ",(0,n.jsx)(s.code,{children:"BroadPhaseFilter"})," and register it (using ",(0,n.jsx)(s.code,{children:"world.BroadPhaseFilter"}),") to handle any collisions with the custom proxy, otherwise an ",(0,n.jsx)(s.code,{children:"InvalidCollisionTypeException"})," is thrown."]}),"\n",(0,n.jsx)(s.h2,{id:"potential-pairs",children:"Potential pairs"}),"\n",(0,n.jsxs)(s.p,{children:["The tree implementation in Jitter needs to be updated using ",(0,n.jsx)(s.code,{children:"tree.Update(bool multiThread, float dt)"}),".\nThis is done automatically for the dynamic tree owned by the world class (",(0,n.jsx)(s.code,{children:"world.DynamicTree"}),").\nThis update process generates information about pairs of proxies which either start overlapping, or start to separate.\nThis 'events' are used to update the ",(0,n.jsx)(s.code,{children:"tree.PotentialPairs"})," hash set, which holds all overlapping pairs.\nInactive pairs of bodies can be pruned from the hashset by calling ",(0,n.jsx)(s.code,{children:"tree.TrimInactivePairs"})," (also done automatically for the dynamic tree owned by the world class).\nThe Jitter ",(0,n.jsx)(s.code,{children:"world"})," class internally uses the potential pairs to gather more detailed collision information of the pairs and also to generate collision response."]}),"\n",(0,n.jsx)(s.h2,{id:"querying-the-tree",children:"Querying the tree"}),"\n",(0,n.jsx)(s.p,{children:"All tree proxies that overlap a given axis aligned box can be queried"}),"\n",(0,n.jsx)(s.pre,{children:(0,n.jsx)(s.code,{className:"language-cs",children:"public void Query<T>(T hits, in JBBox box) where T : ICollection<IDynamicTreeProxy>\n"})}),"\n",(0,n.jsx)(s.p,{children:"as well as all proxies which overlap with a ray"}),"\n",(0,n.jsx)(s.pre,{children:(0,n.jsx)(s.code,{className:"language-cs",children:"public void Query<T>(T hits, JVector rayOrigin, JVector rayDirection) where T : ICollection<IDynamicTreeProxy>\n"})}),"\n",(0,n.jsx)(s.p,{children:"Custom queries can easily be implemented. An implementation which queries all proxies which have an overlap with a single point can be implemented like this:"}),"\n",(0,n.jsx)(s.pre,{children:(0,n.jsx)(s.code,{className:"language-cs",children:"var stack = new Stack<int>();\nstack.Push(tree.Root);\n\nwhile (stack.TryPop(out int id))\n{\n    ref DynamicTree.Node node = ref tree.Nodes[id];\n    \n    if (node.ExpandedBox.Contains(point) != JBBox.ContainmentType.Disjoint)\n    {\n        if (node.IsLeaf)\n        {\n            Console.WriteLine($'{node.Proxy} contains {point}.');\n        }\n        else\n        {\n            stack.Push(node.Left);\n            stack.Push(node.Right);\n        }\n    }\n}\n"})}),"\n",(0,n.jsx)(s.h2,{id:"ray-casting",children:"Ray casting"}),"\n",(0,n.jsxs)(s.p,{children:["All proxies in the tree which implement the ",(0,n.jsx)(s.code,{children:"IRayCastable"})," interface can be raycasted.\nThis includes all shapes:"]}),"\n",(0,n.jsx)(s.pre,{children:(0,n.jsx)(s.code,{className:"language-cs",children:"public bool RayCast(JVector origin, JVector direction, RayCastFilterPre? pre, RayCastFilterPost? post,\n    out IDynamicTreeProxy? proxy, out JVector normal, out float lambda)\n"})}),"\n",(0,n.jsxs)(s.p,{children:["The pre- and post-filters can be used to discard hits during the ray cast.\nJitter shoots a ray from the origin into the specified direction.\nThe function returns ",(0,n.jsx)(s.code,{children:"true"})," if a hit was found.\nIt also reports the point of collision which is given by"]}),"\n",(0,n.jsx)(s.span,{className:"katex-display",children:(0,n.jsxs)(s.span,{className:"katex",children:[(0,n.jsx)(s.span,{className:"katex-mathml",children:(0,n.jsx)(s.math,{xmlns:"http://www.w3.org/1998/Math/MathML",display:"block",children:(0,n.jsxs)(s.semantics,{children:[(0,n.jsxs)(s.mrow,{children:[(0,n.jsxs)(s.mrow,{children:[(0,n.jsx)(s.mi,{mathvariant:"bold",children:"h"}),(0,n.jsx)(s.mi,{mathvariant:"bold",children:"i"}),(0,n.jsx)(s.mi,{mathvariant:"bold",children:"t"})]}),(0,n.jsx)(s.mo,{children:"="}),(0,n.jsxs)(s.mrow,{children:[(0,n.jsx)(s.mi,{mathvariant:"bold",children:"o"}),(0,n.jsx)(s.mi,{mathvariant:"bold",children:"r"}),(0,n.jsx)(s.mi,{mathvariant:"bold",children:"i"}),(0,n.jsx)(s.mi,{mathvariant:"bold",children:"g"}),(0,n.jsx)(s.mi,{mathvariant:"bold",children:"i"}),(0,n.jsx)(s.mi,{mathvariant:"bold",children:"n"})]}),(0,n.jsx)(s.mo,{children:"+"}),(0,n.jsx)(s.mi,{children:"\u03bb"}),(0,n.jsx)(s.mrow,{}),(0,n.jsx)(s.mtext,{children:"\u2009"}),(0,n.jsx)(s.mo,{children:"\xd7"}),(0,n.jsx)(s.mtext,{children:"\u2009"}),(0,n.jsxs)(s.mrow,{children:[(0,n.jsx)(s.mi,{mathvariant:"bold",children:"d"}),(0,n.jsx)(s.mi,{mathvariant:"bold",children:"i"}),(0,n.jsx)(s.mi,{mathvariant:"bold",children:"r"}),(0,n.jsx)(s.mi,{mathvariant:"bold",children:"e"}),(0,n.jsx)(s.mi,{mathvariant:"bold",children:"c"}),(0,n.jsx)(s.mi,{mathvariant:"bold",children:"t"}),(0,n.jsx)(s.mi,{mathvariant:"bold",children:"i"}),(0,n.jsx)(s.mi,{mathvariant:"bold",children:"o"}),(0,n.jsx)(s.mi,{mathvariant:"bold",children:"n"})]}),(0,n.jsx)(s.mo,{separator:"true",children:","}),(0,n.jsx)(s.mspace,{width:"1em"}),(0,n.jsx)(s.mtext,{children:"with"}),(0,n.jsx)(s.mspace,{width:"1em"}),(0,n.jsx)(s.mi,{children:"\u03bb"}),(0,n.jsx)(s.mo,{children:"\u2208"}),(0,n.jsx)(s.mo,{stretchy:"false",children:"["}),(0,n.jsx)(s.mn,{children:"0"}),(0,n.jsx)(s.mo,{separator:"true",children:","}),(0,n.jsx)(s.mi,{mathvariant:"normal",children:"\u221e"}),(0,n.jsx)(s.mo,{stretchy:"false",children:")"}),(0,n.jsx)(s.mi,{mathvariant:"normal",children:"."})]}),(0,n.jsx)(s.annotation,{encoding:"application/x-tex",children:"\\mathbf{hit} = \\mathbf{origin} + \\lambda{}\\,\\times\\,\\mathbf{direction}, \\quad \\textrm{with} \\quad \\lambda \\in [0,\\infty)."})]})})}),(0,n.jsxs)(s.span,{className:"katex-html","aria-hidden":"true",children:[(0,n.jsxs)(s.span,{className:"base",children:[(0,n.jsx)(s.span,{className:"strut",style:{height:"0.6944em"}}),(0,n.jsx)(s.span,{className:"mord",children:(0,n.jsx)(s.span,{className:"mord mathbf",children:"hit"})}),(0,n.jsx)(s.span,{className:"mspace",style:{marginRight:"0.2778em"}}),(0,n.jsx)(s.span,{className:"mrel",children:"="}),(0,n.jsx)(s.span,{className:"mspace",style:{marginRight:"0.2778em"}})]}),(0,n.jsxs)(s.span,{className:"base",children:[(0,n.jsx)(s.span,{className:"strut",style:{height:"0.8889em",verticalAlign:"-0.1944em"}}),(0,n.jsx)(s.span,{className:"mord",children:(0,n.jsx)(s.span,{className:"mord mathbf",children:"origin"})}),(0,n.jsx)(s.span,{className:"mspace",style:{marginRight:"0.2222em"}}),(0,n.jsx)(s.span,{className:"mbin",children:"+"}),(0,n.jsx)(s.span,{className:"mspace",style:{marginRight:"0.2222em"}})]}),(0,n.jsxs)(s.span,{className:"base",children:[(0,n.jsx)(s.span,{className:"strut",style:{height:"0.7778em",verticalAlign:"-0.0833em"}}),(0,n.jsx)(s.span,{className:"mord mathnormal",children:"\u03bb"}),(0,n.jsx)(s.span,{className:"mord"}),(0,n.jsx)(s.span,{className:"mspace",style:{marginRight:"0.1667em"}}),(0,n.jsx)(s.span,{className:"mspace",style:{marginRight:"0.2222em"}}),(0,n.jsx)(s.span,{className:"mbin",children:"\xd7"}),(0,n.jsx)(s.span,{className:"mspace",style:{marginRight:"0.1667em"}}),(0,n.jsx)(s.span,{className:"mspace",style:{marginRight:"0.2222em"}})]}),(0,n.jsxs)(s.span,{className:"base",children:[(0,n.jsx)(s.span,{className:"strut",style:{height:"0.8889em",verticalAlign:"-0.1944em"}}),(0,n.jsx)(s.span,{className:"mord",children:(0,n.jsx)(s.span,{className:"mord mathbf",children:"direction"})}),(0,n.jsx)(s.span,{className:"mpunct",children:","}),(0,n.jsx)(s.span,{className:"mspace",style:{marginRight:"1em"}}),(0,n.jsx)(s.span,{className:"mspace",style:{marginRight:"0.1667em"}}),(0,n.jsx)(s.span,{className:"mord text",children:(0,n.jsx)(s.span,{className:"mord textrm",children:"with"})}),(0,n.jsx)(s.span,{className:"mspace",style:{marginRight:"1em"}}),(0,n.jsx)(s.span,{className:"mord mathnormal",children:"\u03bb"}),(0,n.jsx)(s.span,{className:"mspace",style:{marginRight:"0.2778em"}}),(0,n.jsx)(s.span,{className:"mrel",children:"\u2208"}),(0,n.jsx)(s.span,{className:"mspace",style:{marginRight:"0.2778em"}})]}),(0,n.jsxs)(s.span,{className:"base",children:[(0,n.jsx)(s.span,{className:"strut",style:{height:"1em",verticalAlign:"-0.25em"}}),(0,n.jsx)(s.span,{className:"mopen",children:"["}),(0,n.jsx)(s.span,{className:"mord",children:"0"}),(0,n.jsx)(s.span,{className:"mpunct",children:","}),(0,n.jsx)(s.span,{className:"mspace",style:{marginRight:"0.1667em"}}),(0,n.jsx)(s.span,{className:"mord",children:"\u221e"}),(0,n.jsx)(s.span,{className:"mclose",children:")"}),(0,n.jsx)(s.span,{className:"mord",children:"."})]})]})]})}),"\n",(0,n.jsxs)(s.p,{children:["The returned ",(0,n.jsx)(s.code,{children:"normal"})," is the normalized surface normal at the hit point."]})]})}function o(e={}){const{wrapper:s}={...(0,i.R)(),...e.components};return s?(0,n.jsx)(s,{...e,children:(0,n.jsx)(h,{...e})}):h(e)}},5133:(e,s,a)=>{a.d(s,{A:()=>n});const n=a.p+"assets/images/dynamictree-2a2a0757b4e825e705375cdffc4bc354.png"},8453:(e,s,a)=>{a.d(s,{R:()=>r,x:()=>l});var n=a(6540);const i={},t=n.createContext(i);function r(e){const s=n.useContext(t);return n.useMemo((function(){return"function"==typeof e?e(s):{...s,...e}}),[s,e])}function l(e){let s;return s=e.disableParentContext?"function"==typeof e.components?e.components(i):e.components||i:r(e.components),n.createElement(t.Provider,{value:s},e.children)}}}]);