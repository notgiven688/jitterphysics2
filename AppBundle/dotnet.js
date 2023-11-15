//! Licensed to the .NET Foundation under one or more agreements.
//! The .NET Foundation licenses this file to you under the MIT license.
var __dotnet_runtime=function(e){"use strict";var t="7.0.13",n=false,r="Release";let o,s,i,a,c,u,l,f;const _={},d={};let m;function g(e,t){s=t.internal,i=t.marshaled_imports,o=t.module,w(e),a=e.isNode,c=e.isShell,u=e.isWeb,l=e.isWorker,f=e.isPThread,b.quit=e.quit_,b.ExitStatus=e.ExitStatus,b.requirePromise=e.requirePromise}function w(e){a=e.isNode,c=e.isShell,u=e.isWeb,l=e.isWorker,f=e.isPThread}function h(e){m=e}const p=undefined,b={javaScriptExports:{},mono_wasm_load_runtime_done:false,mono_wasm_bindings_is_ready:false,maxParallelDownloads:16,config:{environmentVariables:{}},diagnosticTracing:false},y=0,v=0,E=0,A=0,S=0,O=0,x=-1,j=0,$=0,N=0,k=0;function T(e){return void 0===e||null===e}const R=[[true,"mono_wasm_register_root","number",["number","number","string"]],[true,"mono_wasm_deregister_root",null,["number"]],[true,"mono_wasm_string_get_data",null,["number","number","number","number"]],[true,"mono_wasm_string_get_data_ref",null,["number","number","number","number"]],[true,"mono_wasm_set_is_debugger_attached","void",["bool"]],[true,"mono_wasm_send_dbg_command","bool",["number","number","number","number","number"]],[true,"mono_wasm_send_dbg_command_with_parms","bool",["number","number","number","number","number","number","string"]],[true,"mono_wasm_setenv",null,["string","string"]],[true,"mono_wasm_parse_runtime_options",null,["number","number"]],[true,"mono_wasm_strdup","number",["string"]],[true,"mono_background_exec",null,[]],[true,"mono_set_timeout_exec",null,[]],[true,"mono_wasm_load_icu_data","number",["number"]],[true,"mono_wasm_get_icudt_name","string",["string"]],[false,"mono_wasm_add_assembly","number",["string","number","number"]],[true,"mono_wasm_add_satellite_assembly","void",["string","string","number","number"]],[false,"mono_wasm_load_runtime",null,["string","number"]],[true,"mono_wasm_change_debugger_log_level","void",["number"]],[true,"mono_wasm_get_corlib","number",[]],[true,"mono_wasm_assembly_load","number",["string"]],[true,"mono_wasm_find_corlib_class","number",["string","string"]],[true,"mono_wasm_assembly_find_class","number",["number","string","string"]],[true,"mono_wasm_runtime_run_module_cctor","void",["number"]],[true,"mono_wasm_find_corlib_type","number",["string","string"]],[true,"mono_wasm_assembly_find_type","number",["number","string","string"]],[true,"mono_wasm_assembly_find_method","number",["number","string","number"]],[true,"mono_wasm_invoke_method","number",["number","number","number","number"]],[false,"mono_wasm_invoke_method_ref","void",["number","number","number","number","number"]],[true,"mono_wasm_string_get_utf8","number",["number"]],[true,"mono_wasm_string_from_utf16_ref","void",["number","number","number"]],[true,"mono_wasm_get_obj_type","number",["number"]],[true,"mono_wasm_array_length","number",["number"]],[true,"mono_wasm_array_get","number",["number","number"]],[true,"mono_wasm_array_get_ref","void",["number","number","number"]],[false,"mono_wasm_obj_array_new","number",["number"]],[false,"mono_wasm_obj_array_new_ref","void",["number","number"]],[false,"mono_wasm_obj_array_set","void",["number","number","number"]],[false,"mono_wasm_obj_array_set_ref","void",["number","number","number"]],[true,"mono_wasm_register_bundled_satellite_assemblies","void",[]],[false,"mono_wasm_try_unbox_primitive_and_get_type_ref","number",["number","number","number"]],[true,"mono_wasm_box_primitive_ref","void",["number","number","number","number"]],[true,"mono_wasm_intern_string_ref","void",["number"]],[true,"mono_wasm_assembly_get_entry_point","number",["number"]],[true,"mono_wasm_get_delegate_invoke_ref","number",["number"]],[true,"mono_wasm_string_array_new_ref","void",["number","number"]],[true,"mono_wasm_typed_array_new_ref","void",["number","number","number","number","number"]],[true,"mono_wasm_class_get_type","number",["number"]],[true,"mono_wasm_type_get_class","number",["number"]],[true,"mono_wasm_get_type_name","string",["number"]],[true,"mono_wasm_get_type_aqn","string",["number"]],[true,"mono_wasm_event_pipe_enable","bool",["string","number","number","string","bool","number"]],[true,"mono_wasm_event_pipe_session_start_streaming","bool",["number"]],[true,"mono_wasm_event_pipe_session_disable","bool",["number"]],[true,"mono_wasm_diagnostic_server_create_thread","bool",["string","number"]],[true,"mono_wasm_diagnostic_server_thread_attach_to_runtime","void",[]],[true,"mono_wasm_diagnostic_server_post_resume_runtime","void",[]],[true,"mono_wasm_diagnostic_server_create_stream","number",[]],[true,"mono_wasm_string_from_js","number",["string"]],[false,"mono_wasm_exit","void",["number"]],[true,"mono_wasm_getenv","number",["string"]],[true,"mono_wasm_set_main_args","void",["number","number"]],[false,"mono_wasm_enable_on_demand_gc","void",["number"]],[false,"mono_profiler_init_aot","void",["number"]],[false,"mono_wasm_exec_regression","number",["number","string"]],[false,"mono_wasm_invoke_method_bound","number",["number","number"]],[true,"mono_wasm_write_managed_pointer_unsafe","void",["number","number"]],[true,"mono_wasm_copy_managed_pointer","void",["number","number"]],[true,"mono_wasm_i52_to_f64","number",["number","number"]],[true,"mono_wasm_u52_to_f64","number",["number","number"]],[true,"mono_wasm_f64_to_i52","number",["number","number"]],[true,"mono_wasm_f64_to_u52","number",["number","number"]]],M={};function I(){const e=!!f;for(const t of R){const n=M,[r,s,i,a,c]=t;if(r||e)n[s]=function(...e){const t=o.cwrap(s,i,a,c);return n[s]=t,t(...e)};else{const e=o.cwrap(s,i,a,c);n[s]=e}}}function D(e,t,n){const r=C(e,t,n);let o="",s=0,i=0,a=0,c=0,u=0,l=0;const f=16777215,_=262143,d=4095,m=63,g=18,w=12,h=6,p=0;for(;s=r.read(),i=r.read(),a=r.read(),null!==s;)null===i&&(i=0,u+=1),null===a&&(a=0,u+=1),l=s<<16|i<<8|a<<0,c=(l&f)>>g,o+=U[c],c=(l&_)>>w,o+=U[c],u<2&&(c=(l&d)>>6,o+=U[c]),2===u?o+="==":1===u?o+="=":(c=(l&m)>>0,o+=U[c]);return o}const U=["A","B","C","D","E","F","G","H","I","J","K","L","M","N","O","P","Q","R","S","T","U","V","W","X","Y","Z","a","b","c","d","e","f","g","h","i","j","k","l","m","n","o","p","q","r","s","t","u","v","w","x","y","z","0","1","2","3","4","5","6","7","8","9","+","/"];function C(e,t,n){let r="number"===typeof t?t:0,o;o="number"===typeof n?r+n:e.length-r;const s={read:function(){if(r>=o)return null;const t=e[r];return r+=1,t}};return Object.defineProperty(s,"eof",{get:function(){return r>=o},configurable:true,enumerable:true}),s}const P=new Map;P.remove=function(e){const t=this.get(e);return this.delete(e),t};let W={},F=0,B=-1,V,H,z;function mono_wasm_runtime_ready(){if(s.mono_wasm_runtime_is_ready=b.mono_wasm_runtime_is_ready=true,F=0,W={},B=-1,globalThis.dotnetDebugger)debugger;else console.debug("mono_wasm_runtime_ready","fe00e07a-5519-4dfe-b35a-f867dbaf2e28")}function mono_wasm_fire_debugger_agent_message(){debugger}function L(e,t,n,r){const s=undefined,i=undefined,a={res_ok:e,res:{id:t,value:D(new Uint8Array(o.HEAPU8.buffer,n,r))}};P.has(t)&&console.warn(`MONO_WASM: Adding an id (${t}) that already exists in commands_received`),P.set(t,a)}function J(e){e.length>B&&(V&&o._free(V),B=Math.max(e.length,B,256),V=o._malloc(B));const t=atob(e);for(let e=0;e<t.length;e++)o.HEAPU8[V+e]=t.charCodeAt(e)}function q(e,t,n,r,o,s,i){J(r),M.mono_wasm_send_dbg_command_with_parms(e,t,n,V,o,s,i.toString());const{res_ok:a,res:c}=P.remove(e);if(!a)throw new Error("Failed on mono_wasm_invoke_method_debugger_agent_with_parms");return c}function G(e,t,n,r){J(r),M.mono_wasm_send_dbg_command(e,t,n,V,r.length);const{res_ok:o,res:s}=P.remove(e);if(!o)throw new Error("Failed on mono_wasm_send_dbg_command");return s}function Y(){const{res_ok:e,res:t}=P.remove(0);if(!e)throw new Error("Failed on mono_wasm_get_dbg_command_info");return t}function Z(){}function X(){M.mono_wasm_set_is_debugger_attached(false)}function Q(e){M.mono_wasm_change_debugger_log_level(e)}function K(e,t={}){if("object"!==typeof e)throw new Error(`event must be an object, but got ${JSON.stringify(e)}`);if(void 0===e.eventName)throw new Error(`event.eventName is a required parameter, in event: ${JSON.stringify(e)}`);if("object"!==typeof t)throw new Error(`args must be an object, but got ${JSON.stringify(t)}`);console.debug("mono_wasm_debug_event_raised:aef14bca-5519-4dfe-b35a-f867abc123ae",JSON.stringify(e),JSON.stringify(t))}function ee(){return new Promise((e=>{const t=setInterval((()=>{1==b.waitForDebugger&&(clearInterval(t),e())}),100)}))}function te(){-1==b.waitForDebugger&&(b.waitForDebugger=1),M.mono_wasm_set_is_debugger_attached(true)}function ne(e,t){H=o.UTF8ToString(e).concat(".dll"),z=t,console.assert(true,`Adding an entrypoint breakpoint ${H} at method token  ${z}`);debugger}function re(e,t){if(e.startsWith("dotnet:array:")){let e;if(void 0===t.items)return e=t.map((e=>e.value)),e;if(void 0===t.dimensionsDetails||1===t.dimensionsDetails.length)return e=t.items.map((e=>e.value)),e}const n={};return Object.keys(t).forEach((e=>{const r=t[e];void 0!==r.get?Object.defineProperty(n,r.name,{get(){return G(r.get.id,r.get.commandSet,r.get.command,r.get.buffer)},set:function(e){return q(r.set.id,r.set.commandSet,r.set.command,r.set.buffer,r.set.length,r.set.valtype,e),true}}):void 0!==r.set?Object.defineProperty(n,r.name,{get(){return r.value},set:function(e){return q(r.set.id,r.set.commandSet,r.set.command,r.set.buffer,r.set.length,r.set.valtype,e),true}}):n[r.name]=r.value})),n}function oe(e){if(void 0!=e.arguments&&!Array.isArray(e.arguments))throw new Error(`"arguments" should be an array, but was ${e.arguments}`);const t=e.objectId,n=e.details;let r={};if(t.startsWith("dotnet:cfo_res:")){if(!(t in W))throw new Error(`Unknown object id ${t}`);r=W[t]}else r=re(t,n);const o=void 0!=e.arguments?e.arguments.map((e=>JSON.stringify(e.value))):[],s=`const fn = ${e.functionDeclaration}; return fn.apply(proxy, [${o}]);`,i=undefined,a=new Function("proxy",s)(r);if(void 0===a)return{type:"undefined"};if(Object(a)!==a)return"object"==typeof a&&null==a?{type:typeof a,subtype:`${a}`,value:null}:{type:typeof a,description:`${a}`,value:`${a}`};if(e.returnByValue&&void 0==a.subtype)return{type:"object",value:a};if(Object.getPrototypeOf(a)==Array.prototype){const e=ae(a);return{type:"object",subtype:"array",className:"Array",description:`Array(${a.length})`,objectId:e}}if(void 0!==a.value||void 0!==a.subtype)return a;if(a==r)return{type:"object",className:"Object",description:"Object",objectId:t};const c=undefined;return{type:"object",className:"Object",description:"Object",objectId:ae(a)}}function se(e,t){if(!(e in W))throw new Error(`Could not find any object with id ${e}`);const n=W[e],r=Object.getOwnPropertyDescriptors(n);t.accessorPropertiesOnly&&Object.keys(r).forEach((e=>{void 0===r[e].get&&Reflect.deleteProperty(r,e)}));const o=[];return Object.keys(r).forEach((e=>{let t;const n=r[e];t="object"==typeof n.value?Object.assign({name:e},n):void 0!==n.value?{name:e,value:Object.assign({type:typeof n.value,description:""+n.value},n)}:void 0!==n.get?{name:e,get:{className:"Function",description:`get ${e} () {}`,type:"function"}}:{name:e,value:{type:"symbol",value:"<Unknown>",description:"<Unknown>"}},o.push(t)})),{__value_as_json_string__:JSON.stringify(o)}}function ie(e,t={}){return se(`dotnet:cfo_res:${e}`,t)}function ae(e){const t="dotnet:cfo_res:"+F++;return W[t]=e,t}function ce(e){e in W&&delete W[e]}function ue(e,t){const n=o.UTF8ToString(t);if(s.logging&&"function"===typeof s.logging.debugger)return s.logging.debugger(e,n),void 0}let le=0;function fe(e){const t=1===M.mono_wasm_load_icu_data(e);return t&&le++,t}function _e(e){return M.mono_wasm_get_icudt_name(e)}function de(){const e=b.config;let t=false;if(e.globalizationMode||(e.globalizationMode="auto"),"invariant"===e.globalizationMode&&(t=true),!t)if(le>0)b.diagnosticTracing&&console.debug("MONO_WASM: ICU data archive(s) loaded, disabling invariant mode");else{if("icu"===e.globalizationMode){const e="invariant globalization mode is inactive and no ICU data archives were loaded";throw o.printErr(`MONO_WASM: ERROR: ${e}`),new Error(e)}b.diagnosticTracing&&console.debug("MONO_WASM: ICU data archive(s) not loaded, using invariant globalization mode"),t=true}t&&M.mono_wasm_setenv("DOTNET_SYSTEM_GLOBALIZATION_INVARIANT","1"),M.mono_wasm_setenv("DOTNET_SYSTEM_GLOBALIZATION_PREDEFINED_CULTURES_ONLY","1")}function me(e){null==e&&(e={}),"writeAt"in e||(e.writeAt="System.Runtime.InteropServices.JavaScript.JavaScriptExports::StopProfile"),"sendTo"in e||(e.sendTo="Interop/Runtime::DumpAotProfileData");const t="aot:write-at-method="+e.writeAt+",send-to-method="+e.sendTo;o.ccall("mono_wasm_load_profiler_aot",null,["string"],[t])}function ge(e){null==e&&(e={}),"writeAt"in e||(e.writeAt="WebAssembly.Runtime::StopProfile"),"sendTo"in e||(e.sendTo="WebAssembly.Runtime::DumpCoverageProfileData");const t="coverage:write-at-method="+e.writeAt+",send-to-method="+e.sendTo;o.ccall("mono_wasm_load_profiler_coverage",null,["string"],[t])}const we=new Map,he=new Map;let pe=0;function be(e){if(we.has(e))return we.get(e);const t=M.mono_wasm_assembly_load(e);return we.set(e,t),t}function ye(e,t,n){let r=he.get(e);r||he.set(e,r=new Map);let o=r.get(t);return o||(o=new Map,r.set(t,o)),o.get(n)}function ve(e,t,n,r){const o=he.get(e);if(!o)throw new Error("internal error");const s=o.get(t);if(!s)throw new Error("internal error");s.set(n,r)}function Ee(e,t,n){pe||(pe=M.mono_wasm_get_corlib());let r=ye(pe,e,t);if(void 0!==r)return r;if(r=M.mono_wasm_assembly_find_class(pe,e,t),n&&!r)throw new Error(`Failed to find corlib class ${e}.${t}`);return ve(pe,e,t,r),r}
//! Licensed to the .NET Foundation under one or more agreements.
const Ae=new Map,Se=[];function Oe(e){try{if(0==Ae.size)return e;const t=e;for(let n=0;n<Se.length;n++){const r=e.replace(new RegExp(Se[n],"g"),((e,...t)=>{const n=t.find((e=>"object"==typeof e&&void 0!==e.replaceSection));if(void 0===n)return e;const r=n.funcNum,o=n.replaceSection,s=Ae.get(Number(r));return void 0===s?e:e.replace(o,`${s} (${o})`)}));if(r!==t)return r}return t}catch(t){return console.debug(`MONO_WASM: failed to symbolicate: ${t}`),e}}function xe(e){let t=e;return t instanceof Error||(t=new Error(t)),Oe(t.stack)}function je(e,t,n,r,i){const a=o.UTF8ToString(n),c=!!r,u=o.UTF8ToString(e),l=i,f=o.UTF8ToString(t),_=`[MONO] ${a}`;if(s.logging&&"function"===typeof s.logging.trace)return s.logging.trace(u,f,_,c,l),void 0;switch(f){case"critical":case"error":console.error(xe(_));break;case"warning":console.warn(_);break;case"message":console.log(_);break;case"info":console.info(_);break;case"debug":console.debug(_);break;default:console.log(_);break}}let $e;function Ne(e,t,n){const r={log:t.log,error:t.error},o=t;function s(t,n,o){return function(...s){try{let r=s[0];if(void 0===r)r="undefined";else if(null===r)r="null";else if("function"===typeof r)r=r.toString();else if("string"!==typeof r)try{r=JSON.stringify(r)}catch(e){r=r.toString()}"string"===typeof r&&"main"!==e&&(r=`[${e}] ${r}`),n(o?JSON.stringify({method:t,payload:r,arguments:s}):[t+r,...s.slice(1)])}catch(e){r.error(`proxyConsole failed: ${e}`)}}}const i=["debug","trace","warn","info","error"];for(const e of i)"function"!==typeof o[e]&&(o[e]=s(`console.${e}: `,t.log,false));const a=`${n}/console`.replace("https://","wss://").replace("http://","ws://");$e=new WebSocket(a),$e.addEventListener("open",(()=>{r.log(`browser: [${e}] Console websocket connected.`)})),$e.addEventListener("error",(t=>{r.error(`[${e}] websocket error: ${t}`,t)})),$e.addEventListener("close",(t=>{r.error(`[${e}] websocket closed: ${t}`,t)}));const c=e=>{$e.readyState===WebSocket.OPEN?$e.send(e):r.log(e)};for(const e of["log",...i])o[e]=s(`console.${e}`,c,true)}function ke(e){if(!b.mono_wasm_symbols_are_ready){b.mono_wasm_symbols_are_ready=true;try{const t=undefined;o.FS_readFile(e,{flags:"r",encoding:"utf8"}).split(/[\r\n]/).forEach((e=>{const t=e.split(/:/);t.length<2||(t[1]=t.splice(1).join(":"),Ae.set(Number(t[0]),t[1]))}))}catch(t){return 44==t.errno||console.log(`MONO_WASM: Error loading symbol file ${e}: ${JSON.stringify(t)}`),void 0}}}async function Te(e,t){try{const n=await Re(e,t);return De(n),n}catch(e){return e instanceof b.ExitStatus?e.status:(De(1,e),1)}}async function Re(e,t){Ic(e,t),-1==b.waitForDebugger&&(console.log("MONO_WASM: waiting for debugger..."),await ee());const n=Me(e);return b.javaScriptExports.call_entry_point(n,t)}function Me(e){if(!b.mono_wasm_bindings_is_ready)throw new Error("Assert failed: The runtime must be initialized.");const t=be(e);if(!t)throw new Error("Could not find assembly: "+e);let n=0;1==b.waitForDebugger&&(n=1);const r=M.mono_wasm_assembly_get_entry_point(t,n);if(!r)throw new Error("Could not find entry point for assembly: "+e);return r}function Ie(e){bc(e,false),De(1,e)}function De(e,t){if(b.config.asyncFlushOnExit&&0===e)throw(async()=>{try{await Ue()}finally{Ce(e,t)}})(),b.ExitStatus?new b.ExitStatus(e):t||new Error("Stop with exit code "+e);Ce(e,t)}async function Ue(){try{const e=await import("process"),t=e=>new Promise(((t,n)=>{e.on("error",(e=>n(e))),e.write("",(function(){t()}))})),n=t(e.stderr),r=t(e.stdout);await Promise.all([r,n])}catch(e){console.error(`flushing std* streams failed: ${e}`)}}function Ce(e,t){if(b.ExitStatus&&(!t||t instanceof b.ExitStatus?t=new b.ExitStatus(e):t instanceof Error?o.printErr(s.mono_wasm_stringify_as_error_with_stack(t)):"string"==typeof t?o.printErr(t):o.printErr(JSON.stringify(t))),We(e,t),Pe(e),0!==e||!u){if(!b.quit)throw t;b.quit(e,t)}}function Pe(e){if(u&&b.config.appendElementOnExit){const t=document.createElement("label");t.id="tests_done",e&&(t.style.background="red"),t.innerHTML=e.toString(),document.body.appendChild(t)}}function We(e,t){if(b.config.logExitCode)if(0!=e&&t&&(t instanceof Error?console.error(xe(t)):"string"==typeof t?console.error(t):console.error(JSON.stringify(t))),$e){const t=()=>{0==$e.bufferedAmount?console.log("WASM EXIT "+e):setTimeout(t,100)};t()}else console.log("WASM EXIT "+e)}Se.push(/at (?<replaceSection>[^:()]+:wasm-function\[(?<funcNum>\d+)\]:0x[a-fA-F\d]+)((?![^)a-fA-F\d])|$)/),Se.push(/(?:WASM \[[\da-zA-Z]+\], (?<replaceSection>function #(?<funcNum>[\d]+) \(''\)))/),Se.push(/(?<replaceSection>[a-z]+:\/\/[^ )]*:wasm-function\[(?<funcNum>\d+)\]:0x[a-fA-F\d]+)/),Se.push(/(?<replaceSection><[^ >]+>[.:]wasm-function\[(?<funcNum>[0-9]+)\])/);const Fe="function"===typeof globalThis.WeakRef;function Be(e){return Fe?new WeakRef(e):{deref:()=>e}}const Ve="function"===typeof globalThis.FinalizationRegistry;let He;const ze=[],Le=[];let Je=1;const qe=new Map;Ve&&(He=new globalThis.FinalizationRegistry(rt));const Ge=Symbol.for("wasm js_owned_gc_handle"),Ye=Symbol.for("wasm cs_owned_js_handle");function Ze(e){return 0!==e&&e!==x?ze[e]:null}function Xe(e){return 0!==e&&e!==x?Ze(e):null}function Qe(e){if(e[Ye])return e[Ye];const t=Le.length?Le.pop():Je++;return ze[t]=e,Object.isExtensible(e)&&(e[Ye]=t),t}function Ke(e){const t=ze[e];if("undefined"!==typeof t&&null!==t){if(globalThis===t)return;"undefined"!==typeof t[Ye]&&(t[Ye]=void 0),ze[e]=void 0,Le.push(e)}}function et(e,t){e[Ge]=t,Ve&&He.register(e,t,e);const n=Be(e);qe.set(t,n)}function tt(e,t){e&&(t=e[Ge],e[Ge]=0,Ve&&He.unregister(e)),0!==t&&qe.delete(t)&&b.javaScriptExports.release_js_owned_object_by_gc_handle(t)}function nt(e){const t=e[Ge];if(!(0!=t))throw new Error("Assert failed: ObjectDisposedException");return t}function rt(e){tt(null,e)}function ot(e){if(!e)return null;const t=qe.get(e);return t?t.deref():null}const st=Symbol.for("wasm promise_control");function it(e,t){let n=null;const r=new Promise((function(r,o){n={isDone:false,promise:null,resolve:t=>{n.isDone||(n.isDone=true,r(t),e&&e())},reject:e=>{n.isDone||(n.isDone=true,o(e),t&&t())}}}));n.promise=r;const o=r;return o[st]=n,{promise:o,promise_control:n}}function at(e){return e[st]}function ct(e){return void 0!==e[st]}function ut(e){if(!ct(e))throw new Error("Assert failed: Promise is not controllable")}const lt=("object"===typeof Promise||"function"===typeof Promise)&&"function"===typeof Promise.resolve;function ft(e){return Promise.resolve(e)===e||("object"===typeof e||"function"===typeof e)&&"function"===typeof e.then}function _t(e){const{promise:t,promise_control:n}=it(),r=undefined;return e().then((e=>n.resolve(e))).catch((e=>n.reject(e))),t}function dt(e){const t=ot(e);if(!t)return;const n=t.promise;if(!!!n)throw new Error(`Assert failed: Expected Promise for GCHandle ${e}`);ut(n);const r=undefined;at(n).reject("OperationCanceledException")}const mt=[],gt=32768;let wt,ht,pt=null;function bt(){wt||(wt=o._malloc(gt),ht=wt)}const yt="undefined"!==typeof BigInt&&"undefined"!==typeof BigInt64Array;function vt(){bt(),mt.push(ht)}function Et(){if(!mt.length)throw new Error("No temp frames have been created at this point");ht=mt.pop()}function At(e,t,n){if(!Number.isSafeInteger(e))throw new Error(`Assert failed: Value is not an integer: ${e} (${typeof e})`);if(!(e>=t&&e<=n))throw new Error(`Assert failed: Overflow: value ${e} is out of ${t} ${n} range`)}function St(e,t){o.HEAP8.fill(0,e,t+e)}function Ot(e,t){const n=!!t;"number"===typeof t&&At(t,0,1),o.HEAP32[e>>>2]=n?1:0}function xt(e,t){At(t,0,255),o.HEAPU8[e]=t}function jt(e,t){At(t,0,65535),o.HEAPU16[e>>>1]=t}function $t(e,t){o.HEAPU32[e>>>2]=t}function Nt(e,t){At(t,0,4294967295),o.HEAPU32[e>>>2]=t}function kt(e,t){At(t,-128,127),o.HEAP8[e]=t}function Tt(e,t){At(t,-32768,32767),o.HEAP16[e>>>1]=t}function Rt(e,t){o.HEAP32[e>>>2]=t}function Mt(e,t){At(t,-2147483648,2147483647),o.HEAP32[e>>>2]=t}function It(e){if(0!==e)switch(e){case 1:throw new Error("value was not an integer");case 2:throw new Error("value out of range");default:throw new Error("unknown internal error")}}function Dt(e,t){if(!Number.isSafeInteger(t))throw new Error(`Assert failed: Value is not a safe integer: ${t} (${typeof t})`);const n=undefined;It(M.mono_wasm_f64_to_i52(e,t))}function Ut(e,t){if(!Number.isSafeInteger(t))throw new Error(`Assert failed: Value is not a safe integer: ${t} (${typeof t})`);if(!(t>=0))throw new Error("Assert failed: Can't convert negative Number into UInt64");const n=undefined;It(M.mono_wasm_f64_to_u52(e,t))}function Ct(e,t){if(!yt)throw new Error("Assert failed: BigInt is not supported.");if(!("bigint"===typeof t))throw new Error(`Assert failed: Value is not an bigint: ${t} (${typeof t})`);if(!(t>=Kt&&t<=Qt))throw new Error(`Assert failed: Overflow: value ${t} is out of ${Kt} ${Qt} range`);pt[e>>>3]=t}function Pt(e,t){if(!("number"===typeof t))throw new Error(`Assert failed: Value is not a Number: ${t} (${typeof t})`);o.HEAPF32[e>>>2]=t}function Wt(e,t){if(!("number"===typeof t))throw new Error(`Assert failed: Value is not a Number: ${t} (${typeof t})`);o.HEAPF64[e>>>3]=t}function Ft(e){return!!o.HEAP32[e>>>2]}function Bt(e){return o.HEAPU8[e]}function Vt(e){return o.HEAPU16[e>>>1]}function Ht(e){return o.HEAPU32[e>>>2]}function zt(e){return o.HEAP8[e]}function Lt(e){return o.HEAP16[e>>>1]}function Jt(e){return o.HEAP32[e>>>2]}function qt(e){const t=M.mono_wasm_i52_to_f64(e,b._i52_error_scratch_buffer),n=undefined;return It(Jt(b._i52_error_scratch_buffer)),t}function Gt(e){const t=M.mono_wasm_u52_to_f64(e,b._i52_error_scratch_buffer),n=undefined;return It(Jt(b._i52_error_scratch_buffer)),t}function Yt(e){if(!yt)throw new Error("Assert failed: BigInt is not supported.");return pt[e>>>3]}function Zt(e){return o.HEAPF32[e>>>2]}function Xt(e){return o.HEAPF64[e>>>3]}let Qt,Kt;function en(e){yt&&(Qt=BigInt("9223372036854775807"),Kt=BigInt("-9223372036854775808"),pt=new BigInt64Array(e))}function tn(e){const t=o._malloc(e.length),n=undefined;return new Uint8Array(o.HEAPU8.buffer,t,e.length).set(e),t}const nn=8192;let rn=null,on=null,sn=0;const an=[],cn=[];function un(e,t){if(e<=0)throw new Error("capacity >= 1");const n=4*(e|=0),r=o._malloc(n);if(r%4!==0)throw new Error("Malloc returned an unaligned offset");return St(r,n),new WasmRootBufferImpl(r,e,true,t)}function ln(e){let t;if(!e)throw new Error("address must be a location in the native heap");return cn.length>0?(t=cn.pop(),t._set_address(e)):t=new wn(e),t}function fn(e){let t;if(an.length>0)t=an.pop();else{const e=mn(),n=undefined;t=new gn(rn,e)}if(void 0!==e){if("number"!==typeof e)throw new Error("value must be an address in the managed heap");t.set(e)}else t.set(0);return t}function _n(...e){for(let t=0;t<e.length;t++)T(e[t])||e[t].release()}function dn(e){void 0!==e&&(rn.set(e,0),on[sn]=e,sn++)}function mn(){if(T(rn)||!on){rn=un(nn,"js roots"),on=new Int32Array(nn),sn=nn;for(let e=0;e<nn;e++)on[e]=nn-e-1}if(sn<1)throw new Error("Out of scratch root space");const e=on[sn-1];return sn--,e}class WasmRootBufferImpl{constructor(e,t,n,r){const o=4*t;this.__offset=e,this.__offset32=e>>>2,this.__count=t,this.length=t,this.__handle=M.mono_wasm_register_root(e,o,r||"noname"),this.__ownsAllocation=n}_throw_index_out_of_range(){throw new Error("index out of range")}_check_in_range(e){(e>=this.__count||e<0)&&this._throw_index_out_of_range()}get_address(e){return this._check_in_range(e),this.__offset+4*e}get_address_32(e){return this._check_in_range(e),this.__offset32+e}get(e){this._check_in_range(e);const t=this.get_address_32(e);return o.HEAPU32[t]}set(e,t){const n=this.get_address(e);return M.mono_wasm_write_managed_pointer_unsafe(n,t),t}copy_value_from_address(e,t){const n=this.get_address(e);M.mono_wasm_copy_managed_pointer(n,t)}_unsafe_get(e){return o.HEAPU32[this.__offset32+e]}_unsafe_set(e,t){const n=this.__offset+e;M.mono_wasm_write_managed_pointer_unsafe(n,t)}clear(){this.__offset&&St(this.__offset,4*this.__count)}release(){this.__offset&&this.__ownsAllocation&&(M.mono_wasm_deregister_root(this.__offset),St(this.__offset,4*this.__count),o._free(this.__offset)),this.__handle=this.__offset=this.__count=this.__offset32=0}toString(){return`[root buffer @${this.get_address(0)}, size ${this.__count} ]`}}class gn{constructor(e,t){this.__buffer=e,this.__index=t}get_address(){return this.__buffer.get_address(this.__index)}get_address_32(){return this.__buffer.get_address_32(this.__index)}get address(){return this.__buffer.get_address(this.__index)}get(){const e=undefined;return this.__buffer._unsafe_get(this.__index)}set(e){const t=this.__buffer.get_address(this.__index);return M.mono_wasm_write_managed_pointer_unsafe(t,e),e}copy_from(e){const t=e.address,n=this.address;M.mono_wasm_copy_managed_pointer(n,t)}copy_to(e){const t=this.address,n=e.address;M.mono_wasm_copy_managed_pointer(n,t)}copy_from_address(e){const t=this.address;M.mono_wasm_copy_managed_pointer(t,e)}copy_to_address(e){const t=this.address;M.mono_wasm_copy_managed_pointer(e,t)}get value(){return this.get()}set value(e){this.set(e)}valueOf(){throw new Error("Implicit conversion of roots to pointers is no longer supported. Use .value or .address as appropriate")}clear(){this.set(0)}release(){if(!this.__buffer)throw new Error("No buffer");const e=128;an.length>e?(dn(this.__index),this.__buffer=null,this.__index=0):(this.set(0),an.push(this))}toString(){return`[root @${this.address}]`}}class wn{constructor(e){this.__external_address=0,this.__external_address_32=0,this._set_address(e)}_set_address(e){this.__external_address=e,this.__external_address_32=e>>>2}get address(){return this.__external_address}get_address(){return this.__external_address}get_address_32(){return this.__external_address_32}get(){const e=undefined;return o.HEAPU32[this.__external_address_32]}set(e){return M.mono_wasm_write_managed_pointer_unsafe(this.__external_address,e),e}copy_from(e){const t=e.address,n=this.__external_address;M.mono_wasm_copy_managed_pointer(n,t)}copy_to(e){const t=this.__external_address,n=e.address;M.mono_wasm_copy_managed_pointer(n,t)}copy_from_address(e){const t=this.__external_address;M.mono_wasm_copy_managed_pointer(t,e)}copy_to_address(e){const t=this.__external_address;M.mono_wasm_copy_managed_pointer(e,t)}get value(){return this.get()}set value(e){this.set(e)}valueOf(){throw new Error("Implicit conversion of roots to pointers is no longer supported. Use .value or .address as appropriate")}clear(){this.set(0)}release(){const e=128;cn.length<e&&cn.push(this)}toString(){return`[external root @${this.address}]`}}const hn=new Map,pn=new Map,bn=Symbol.for("wasm bound_cs_function"),yn=Symbol.for("wasm bound_js_function"),vn=16,En=32,An=8;function Sn(e){const t=undefined,n=o.stackAlloc(vn*e);if(!(n&&n%8==0))throw new Error("Assert failed: Arg alignment");const r=undefined;Cn(On(n,0),wr.None);const s=undefined;return Cn(On(n,1),wr.None),n}function On(e,t){if(!e)throw new Error("Assert failed: Null args");return e+t*vn}function xn(e){if(!e)throw new Error("Assert failed: Null args");const t=undefined;return Dn(e)!==wr.None}function jn(e,t){if(!e)throw new Error("Assert failed: Null signatures");return e+t*En+8}function $n(e){if(!e)throw new Error("Assert failed: Null sig");return Ht(e)}function Nn(e){if(!e)throw new Error("Assert failed: Null sig");return Ht(e+16)}function kn(e){if(!e)throw new Error("Assert failed: Null sig");return Ht(e+20)}function Tn(e){if(!e)throw new Error("Assert failed: Null sig");return Ht(e+24)}function Rn(e){if(!e)throw new Error("Assert failed: Null sig");return Ht(e+28)}function Mn(e){if(!e)throw new Error("Assert failed: Null signatures");return Jt(e+4)}function In(e){if(!e)throw new Error("Assert failed: Null signatures");return Jt(e)}function Dn(e){if(!e)throw new Error("Assert failed: Null arg");const t=undefined;return Ht(e+12)}function Un(e){if(!e)throw new Error("Assert failed: Null arg");const t=undefined;return Ht(e+4)}function Cn(e,t){if(!e)throw new Error("Assert failed: Null arg");Nt(e+12,t)}function Pn(e,t){if(!e)throw new Error("Assert failed: Null arg");Nt(e+4,t)}function Wn(e){if(!e)throw new Error("Assert failed: Null arg");return!!Bt(e)}function Fn(e){if(!e)throw new Error("Assert failed: Null arg");return Bt(e)}function Bn(e){if(!e)throw new Error("Assert failed: Null arg");return Vt(e)}function Vn(e){if(!e)throw new Error("Assert failed: Null arg");return Lt(e)}function Hn(e){if(!e)throw new Error("Assert failed: Null arg");return Jt(e)}function zn(e){if(!e)throw new Error("Assert failed: Null arg");return Ht(e)}function Ln(e){if(!e)throw new Error("Assert failed: Null arg");return Xt(e)}function Jn(e){if(!e)throw new Error("Assert failed: Null arg");return Yt(e)}function qn(e){if(!e)throw new Error("Assert failed: Null arg");const t=Xt(e),n=undefined;return new Date(t)}function Gn(e){if(!e)throw new Error("Assert failed: Null arg");return Zt(e)}function Yn(e){if(!e)throw new Error("Assert failed: Null arg");return Xt(e)}function Zn(e,t){if(!e)throw new Error("Assert failed: Null arg");if(!("boolean"===typeof t))throw new Error(`Assert failed: Value is not a Boolean: ${t} (${typeof t})`);xt(e,t?1:0)}function Xn(e,t){if(!e)throw new Error("Assert failed: Null arg");xt(e,t)}function Qn(e,t){if(!e)throw new Error("Assert failed: Null arg");jt(e,t)}function Kn(e,t){if(!e)throw new Error("Assert failed: Null arg");Tt(e,t)}function er(e,t){if(!e)throw new Error("Assert failed: Null arg");Mt(e,t)}function tr(e,t){if(!e)throw new Error("Assert failed: Null arg");Nt(e,t)}function nr(e,t){if(!e)throw new Error("Assert failed: Null arg");if(!Number.isSafeInteger(t))throw new Error(`Assert failed: Value is not an integer: ${t} (${typeof t})`);Wt(e,t)}function rr(e,t){if(!e)throw new Error("Assert failed: Null arg");Ct(e,t)}function or(e,t){if(!e)throw new Error("Assert failed: Null arg");const n=undefined;Wt(e,t.getTime())}function sr(e,t){if(!e)throw new Error("Assert failed: Null arg");Wt(e,t)}function ir(e,t){if(!e)throw new Error("Assert failed: Null arg");Pt(e,t)}function ar(e){if(!e)throw new Error("Assert failed: Null arg");return Ht(e+4)}function cr(e,t){if(!e)throw new Error("Assert failed: Null arg");Nt(e+4,t)}function ur(e){if(!e)throw new Error("Assert failed: Null arg");return Ht(e+4)}function lr(e,t){if(!e)throw new Error("Assert failed: Null arg");Nt(e+4,t)}function fr(e){if(!e)throw new Error("Assert failed: Null arg");return ln(e)}function _r(e){if(!e)throw new Error("Assert failed: Null arg");return Jt(e+8)}function dr(e,t){if(!e)throw new Error("Assert failed: Null arg");Mt(e+8,t)}class ManagedObject{dispose(){tt(this,0)}get isDisposed(){return 0===this[Ge]}toString(){return`CsObject(gc_handle: ${this[Ge]})`}}class ManagedError extends Error{constructor(e){super(e),this.superStack=Object.getOwnPropertyDescriptor(this,"stack"),Object.defineProperty(this,"stack",{get:this.getManageStack})}getSuperStack(){return this.superStack?this.superStack.value:super.stack}getManageStack(){const e=this[Ge];if(e){const t=b.javaScriptExports.get_managed_stack_trace(e);if(t)return t+"\n"+this.getSuperStack()}return this.getSuperStack()}dispose(){tt(this,0)}get isDisposed(){return 0===this[Ge]}}function mr(e){return e==wr.Byte?1:e==wr.Int32?4:e==wr.Int52||e==wr.Double?8:e==wr.String||e==wr.Object||e==wr.JSObject?vn:-1}class gr{constructor(e,t,n){this._pointer=e,this._length=t,this._viewType=n}_unsafe_create_view(){const e=0==this._viewType?new Uint8Array(o.HEAPU8.buffer,this._pointer,this._length):1==this._viewType?new Int32Array(o.HEAP32.buffer,this._pointer,this._length):2==this._viewType?new Float64Array(o.HEAPF64.buffer,this._pointer,this._length):null;if(!e)throw new Error("NotImplementedException");return e}set(e,t){if(!!this.isDisposed)throw new Error("Assert failed: ObjectDisposedException");const n=this._unsafe_create_view();if(!(e&&n&&e.constructor===n.constructor))throw new Error(`Assert failed: Expected ${n.constructor}`);n.set(e,t)}copyTo(e,t){if(!!this.isDisposed)throw new Error("Assert failed: ObjectDisposedException");const n=this._unsafe_create_view();if(!(e&&n&&e.constructor===n.constructor))throw new Error(`Assert failed: Expected ${n.constructor}`);const r=n.subarray(t);e.set(r)}slice(e,t){if(!!this.isDisposed)throw new Error("Assert failed: ObjectDisposedException");const n=undefined;return this._unsafe_create_view().slice(e,t)}get length(){if(!!this.isDisposed)throw new Error("Assert failed: ObjectDisposedException");return this._length}get byteLength(){if(!!this.isDisposed)throw new Error("Assert failed: ObjectDisposedException");return 0==this._viewType?this._length:1==this._viewType?this._length<<2:2==this._viewType?this._length<<3:0}}class Span extends gr{constructor(e,t,n){super(e,t,n),this.is_disposed=false}dispose(){this.is_disposed=true}get isDisposed(){return this.is_disposed}}class ArraySegment extends gr{constructor(e,t,n){super(e,t,n)}dispose(){tt(this,0)}get isDisposed(){return 0===this[Ge]}}var wr;(function(e){e[e.None=0]="None",e[e.Void=1]="Void",e[e.Discard=2]="Discard",e[e.Boolean=3]="Boolean",e[e.Byte=4]="Byte",e[e.Char=5]="Char",e[e.Int16=6]="Int16",e[e.Int32=7]="Int32",e[e.Int52=8]="Int52",e[e.BigInt64=9]="BigInt64",e[e.Double=10]="Double",e[e.Single=11]="Single",e[e.IntPtr=12]="IntPtr",e[e.JSObject=13]="JSObject",e[e.Object=14]="Object",e[e.String=15]="String",e[e.Exception=16]="Exception",e[e.DateTime=17]="DateTime",e[e.DateTimeOffset=18]="DateTimeOffset",e[e.Nullable=19]="Nullable",e[e.Task=20]="Task",e[e.Array=21]="Array",e[e.ArraySegment=22]="ArraySegment",e[e.Span=23]="Span",e[e.Action=24]="Action",e[e.Function=25]="Function",e[e.JSException=26]="JSException"})(wr||(wr={}));class hr{init_fields(){this.mono_wasm_string_decoder_buffer||(this.mono_text_decoder="undefined"!==typeof TextDecoder?new TextDecoder("utf-16le"):null,this.mono_wasm_string_root=fn(),this.mono_wasm_string_decoder_buffer=o._malloc(12))}copy(e){if(this.init_fields(),0===e)return null;this.mono_wasm_string_root.value=e;const t=this.copy_root(this.mono_wasm_string_root);return this.mono_wasm_string_root.value=0,t}copy_root(e){if(this.init_fields(),0===e.value)return null;const t=this.mono_wasm_string_decoder_buffer+0,n=this.mono_wasm_string_decoder_buffer+4,r=this.mono_wasm_string_decoder_buffer+8;let o;M.mono_wasm_string_get_data_ref(e.address,t,n,r);const s=Jt(n),i=Ht(t),a=Jt(r);if(a&&(o=pr.get(e.value)),void 0===o&&(s&&i?(o=this.decode(i,i+s),a&&pr.set(e.value,o)):o=Sr),void 0===o)throw new Error(`internal error when decoding string at location ${e.value}`);return o}decode(e,t){let n="";if(this.mono_text_decoder){const r="undefined"!==typeof SharedArrayBuffer&&o.HEAPU8.buffer instanceof SharedArrayBuffer?o.HEAPU8.slice(e,t):o.HEAPU8.subarray(e,t);n=this.mono_text_decoder.decode(r)}else for(let r=0;r<t-e;r+=2){const t=o.getValue(e+r,"i16");n+=String.fromCharCode(t)}return n}}const pr=new Map,br=new Map;let yr=0,vr=null,Er=0;const Ar=new hr,Sr="";function Or(e){return Ar.copy(e)}function xr(e){return Ar.copy_root(e)}function jr(e){if(0===e.length)return Sr;const t=Rr(e),n=pr.get(t);if(T(n))throw new Error("internal error: interned_string_table did not contain string after js_string_to_mono_string_interned");return n}function $r(e,t,n){if(!t.value)throw new Error("null pointer passed to _store_string_in_intern_table");const r=8192;Er>=r&&(vr=null),vr||(vr=un(r,"interned strings"),Er=0);const o=vr,s=Er++;if(n&&(M.mono_wasm_intern_string_ref(t.address),!t.value))throw new Error("mono_wasm_intern_string_ref produced a null pointer");br.set(e,t.value),pr.set(t.value,e),0!==e.length||yr||(yr=t.value),o.copy_value_from_address(s,t.address)}function Nr(e,t){let n;if("symbol"===typeof e?(n=e.description,"string"!==typeof n&&(n=Symbol.keyFor(e)),"string"!==typeof n&&(n="<unknown Symbol>")):"string"===typeof e&&(n=e),"string"!==typeof n)throw new Error(`Argument to js_string_to_mono_string_interned must be a string but was ${e}`);if(0===n.length&&yr)return t.set(yr),void 0;const r=br.get(n);if(r)return t.set(r),void 0;Tr(n,t),$r(n,t,true)}function kr(e,t){if(t.clear(),null!==e)if("symbol"===typeof e)Nr(e,t);else{if("string"!==typeof e)throw new Error("Expected string argument, got "+typeof e);if(0===e.length)Nr(e,t);else{if(e.length<=256){const n=br.get(e);if(n)return t.set(n),void 0}Tr(e,t)}}}function Tr(e,t){const n=o._malloc(2*(e.length+1)),r=n>>>1|0;for(let t=0;t<e.length;t++)o.HEAP16[r+t]=e.charCodeAt(t);o.HEAP16[r+e.length]=0,M.mono_wasm_string_from_utf16_ref(n,e.length,t.address),o._free(n)}function Rr(e){const t=fn();try{return Nr(e,t),t.value}finally{t.release()}}function Mr(e){const t=fn();try{return kr(e,t),t.value}finally{t.release()}}function Ir(){0==pn.size&&(pn.set(wr.Array,ro),pn.set(wr.Span,so),pn.set(wr.ArraySegment,io),pn.set(wr.Boolean,Ur),pn.set(wr.Byte,Cr),pn.set(wr.Char,Pr),pn.set(wr.Int16,Wr),pn.set(wr.Int32,Fr),pn.set(wr.Int52,Br),pn.set(wr.BigInt64,Vr),pn.set(wr.Double,Hr),pn.set(wr.Single,zr),pn.set(wr.IntPtr,Lr),pn.set(wr.DateTime,Jr),pn.set(wr.DateTimeOffset,qr),pn.set(wr.String,Gr),pn.set(wr.Exception,eo),pn.set(wr.JSException,eo),pn.set(wr.JSObject,to),pn.set(wr.Object,no),pn.set(wr.Task,Kr),pn.set(wr.Action,Xr),pn.set(wr.Function,Xr),pn.set(wr.None,Zr),pn.set(wr.Discard,Zr),pn.set(wr.Void,Zr))}function Dr(e,t,n,r,o,s){let i="",a="",c="";const u="converter"+t;let l="null",f="null",_="null",d="null",m=$n(e);if(m===wr.None||m===wr.Void)return{converters:i,call_body:c,marshaler_type:m};const g=Nn(e);if(g!==wr.None){const e=pn.get(g);if(!(e&&"function"===typeof e))throw new Error(`Assert failed: Unknow converter for type ${g} at ${t}`);m!=wr.Nullable?(d="converter"+t+"_res",i+=", "+d,a+=" "+wr[g],s[d]=e):m=g}const w=kn(e);if(w!==wr.None){const e=hn.get(w);if(!(e&&"function"===typeof e))throw new Error(`Assert failed: Unknow converter for type ${w} at ${t}`);l="converter"+t+"_arg1",i+=", "+l,a+=" "+wr[w],s[l]=e}const h=Tn(e);if(h!==wr.None){const e=hn.get(h);if(!(e&&"function"===typeof e))throw new Error(`Assert failed: Unknow converter for type ${h} at ${t}`);f="converter"+t+"_arg2",i+=", "+f,a+=" "+wr[h],s[f]=e}const p=Rn(e);if(p!==wr.None){const e=hn.get(p);if(!(e&&"function"===typeof e))throw new Error(`Assert failed: Unknow converter for type ${p} at ${t}`);_="converter"+t+"_arg3",i+=", "+_,a+=" "+wr[p],s[_]=e}const b=pn.get(m),y=wr[m];if(!(b&&"function"===typeof b))throw new Error(`Assert failed: Unknow converter for type ${y} (${m}) at ${t} `);return i+=", "+u,a+=" "+y,s[u]=b,c=m==wr.Task?`  ${u}(args + ${n}, ${o}, signature + ${r}, ${d}); // ${a} \n`:m==wr.Action||m==wr.Function?`  ${u}(args + ${n}, ${o}, signature + ${r}, ${d}, ${l}, ${f}, ${f}); // ${a} \n`:`  ${u}(args + ${n}, ${o}, signature + ${r}); // ${a} \n`,{converters:i,call_body:c,marshaler_type:m}}function Ur(e,t){null===t||void 0===t?Cn(e,wr.None):(Cn(e,wr.Boolean),Zn(e,t))}function Cr(e,t){null===t||void 0===t?Cn(e,wr.None):(Cn(e,wr.Byte),Xn(e,t))}function Pr(e,t){null===t||void 0===t?Cn(e,wr.None):(Cn(e,wr.Char),Qn(e,t))}function Wr(e,t){null===t||void 0===t?Cn(e,wr.None):(Cn(e,wr.Int16),Kn(e,t))}function Fr(e,t){null===t||void 0===t?Cn(e,wr.None):(Cn(e,wr.Int32),er(e,t))}function Br(e,t){null===t||void 0===t?Cn(e,wr.None):(Cn(e,wr.Int52),nr(e,t))}function Vr(e,t){null===t||void 0===t?Cn(e,wr.None):(Cn(e,wr.BigInt64),rr(e,t))}function Hr(e,t){null===t||void 0===t?Cn(e,wr.None):(Cn(e,wr.Double),sr(e,t))}function zr(e,t){null===t||void 0===t?Cn(e,wr.None):(Cn(e,wr.Single),ir(e,t))}function Lr(e,t){null===t||void 0===t?Cn(e,wr.None):(Cn(e,wr.IntPtr),tr(e,t))}function Jr(e,t){if(null===t||void 0===t)Cn(e,wr.None);else{if(!(t instanceof Date))throw new Error("Assert failed: Value is not a Date");Cn(e,wr.DateTime),or(e,t)}}function qr(e,t){if(null===t||void 0===t)Cn(e,wr.None);else{if(!(t instanceof Date))throw new Error("Assert failed: Value is not a Date");Cn(e,wr.DateTimeOffset),or(e,t)}}function Gr(e,t){if(null===t||void 0===t)Cn(e,wr.None);else{if(Cn(e,wr.String),!("string"===typeof t))throw new Error("Assert failed: Value is not a String");Yr(e,t)}}function Yr(e,t){const n=fr(e);try{kr(t,n)}finally{n.release()}}function Zr(e){Cn(e,wr.None)}function Xr(e,t,n,r,o,s,i){if(null===t||void 0===t)return Cn(e,wr.None),void 0;if(!(t&&t instanceof Function))throw new Error("Assert failed: Value is not a Function");const a=e=>{const n=On(e,0),a=On(e,1),c=On(e,2),u=On(e,3),l=On(e,4);try{let e,n,f;o&&(e=o(c)),s&&(n=s(u)),i&&(f=i(l));const _=t(e,n,f);r&&r(a,_)}catch(e){eo(n,e)}};a[yn]=true;const c=undefined;cr(e,Qe(a)),Cn(e,wr.Function)}class Qr{constructor(e){this.promise=e}dispose(){tt(this,0)}get isDisposed(){return 0===this[Ge]}}function Kr(e,t,n,r){if(null===t||void 0===t)return Cn(e,wr.None),void 0;if(!ft(t))throw new Error("Assert failed: Value is not a Promise");const o=b.javaScriptExports.create_task_callback();lr(e,o),Cn(e,wr.Task);const s=new Qr(t);et(s,o),t.then((e=>{b.javaScriptExports.complete_task(o,null,e,r||no),tt(s,o)})).catch((e=>{b.javaScriptExports.complete_task(o,e,null,void 0),tt(s,o)}))}function eo(e,t){if(null===t||void 0===t)Cn(e,wr.None);else if(t instanceof ManagedError){Cn(e,wr.Exception);const n=undefined;lr(e,nt(t))}else{if(!("object"===typeof t||"string"===typeof t))throw new Error("Assert failed: Value is not an Error "+typeof t);Cn(e,wr.JSException);const n=undefined;Yr(e,t.toString());const r=t[Ye];if(r)cr(e,r);else{const n=undefined;cr(e,Qe(t))}}}function to(e,t){if(void 0===t||null===t)Cn(e,wr.None);else{if(!(void 0===t[Ge]))throw new Error("Assert failed: JSObject proxy of ManagedObject proxy is not supported");if(!("function"===typeof t||"object"===typeof t))throw new Error(`Assert failed: JSObject proxy of ${typeof t} is not supported`);Cn(e,wr.JSObject);const n=undefined;cr(e,Qe(t))}}function no(e,t){if(void 0===t||null===t)Cn(e,wr.None);else{const n=t[Ge],r=typeof t;if(void 0===n)if("string"===r||"symbol"===r)Cn(e,wr.String),Yr(e,t);else if("number"===r)Cn(e,wr.Double),sr(e,t);else{if("bigint"===r)throw new Error("NotImplementedException: bigint");if("boolean"===r)Cn(e,wr.Boolean),Zn(e,t);else if(t instanceof Date)Cn(e,wr.DateTime),or(e,t);else if(t instanceof Error)eo(e,t);else if(t instanceof Uint8Array)oo(e,t,wr.Byte);else if(t instanceof Float64Array)oo(e,t,wr.Double);else if(t instanceof Int32Array)oo(e,t,wr.Int32);else if(Array.isArray(t))oo(e,t,wr.Object);else{if(t instanceof Int16Array||t instanceof Int8Array||t instanceof Uint8ClampedArray||t instanceof Uint16Array||t instanceof Uint32Array||t instanceof Float32Array)throw new Error("NotImplementedException: TypedArray");if(ft(t))Kr(e,t);else{if(t instanceof Span)throw new Error("NotImplementedException: Span");if("object"!=r)throw new Error(`JSObject proxy is not supported for ${r} ${t}`);{const n=Qe(t);Cn(e,wr.JSObject),cr(e,n)}}}}else{if(nt(t),t instanceof ArraySegment)throw new Error("NotImplementedException: ArraySegment");if(t instanceof ManagedError)Cn(e,wr.Exception),lr(e,n);else{if(!(t instanceof ManagedObject))throw new Error("NotImplementedException "+r);Cn(e,wr.Object),lr(e,n)}}}}function ro(e,t,n){if(!!!n)throw new Error("Assert failed: Expected valid sig parameter");const r=undefined;oo(e,t,kn(n))}function oo(e,t,n){if(null===t||void 0===t)Cn(e,wr.None);else{const r=mr(n);if(!(-1!=r))throw new Error(`Assert failed: Element type ${wr[n]} not supported`);const s=t.length,i=r*s,a=o._malloc(i);if(n==wr.String){if(!Array.isArray(t))throw new Error("Assert failed: Value is not an Array");St(a,i),M.mono_wasm_register_root(a,i,"marshal_array_to_cs");for(let e=0;e<s;e++){const n=undefined;Gr(On(a,e),t[e])}}else if(n==wr.Object){if(!Array.isArray(t))throw new Error("Assert failed: Value is not an Array");St(a,i),M.mono_wasm_register_root(a,i,"marshal_array_to_cs");for(let e=0;e<s;e++){const n=undefined;no(On(a,e),t[e])}}else if(n==wr.JSObject){if(!Array.isArray(t))throw new Error("Assert failed: Value is not an Array");St(a,i);for(let e=0;e<s;e++){const n=undefined;to(On(a,e),t[e])}}else if(n==wr.Byte){if(!(Array.isArray(t)||t instanceof Uint8Array))throw new Error("Assert failed: Value is not an Array or Uint8Array");const e=undefined;o.HEAPU8.subarray(a,a+s).set(t)}else if(n==wr.Int32){if(!(Array.isArray(t)||t instanceof Int32Array))throw new Error("Assert failed: Value is not an Array or Int32Array");const e=undefined;o.HEAP32.subarray(a>>2,(a>>2)+s).set(t)}else{if(n!=wr.Double)throw new Error("not implemented");{if(!(Array.isArray(t)||t instanceof Float64Array))throw new Error("Assert failed: Value is not an Array or Float64Array");const e=undefined;o.HEAPF64.subarray(a>>3,(a>>3)+s).set(t)}}tr(e,a),Cn(e,wr.Array),Pn(e,n),dr(e,t.length)}}function so(e,t,n){if(!!!n)throw new Error("Assert failed: Expected valid sig parameter");if(!!t.isDisposed)throw new Error("Assert failed: ObjectDisposedException");ao(n,t._viewType),Cn(e,wr.Span),tr(e,t._pointer),dr(e,t.length)}function io(e,t,n){if(!!!n)throw new Error("Assert failed: Expected valid sig parameter");const r=nt(t);if(!r)throw new Error("Assert failed: Only roundtrip of ArraySegment instance created by C#");ao(n,t._viewType),Cn(e,wr.ArraySegment),tr(e,t._pointer),dr(e,t.length),lr(e,r)}function ao(e,t){const n=kn(e);if(n==wr.Byte){if(!(0==t))throw new Error("Assert failed: Expected MemoryViewType.Byte")}else if(n==wr.Int32){if(!(1==t))throw new Error("Assert failed: Expected MemoryViewType.Int32")}else{if(n!=wr.Double)throw new Error(`NotImplementedException ${wr[n]} `);if(!(2==t))throw new Error("Assert failed: Expected MemoryViewType.Double")}}function co(){0==hn.size&&(hn.set(wr.Array,ko),hn.set(wr.Span,Ro),hn.set(wr.ArraySegment,Mo),hn.set(wr.Boolean,lo),hn.set(wr.Byte,fo),hn.set(wr.Char,_o),hn.set(wr.Int16,mo),hn.set(wr.Int32,go),hn.set(wr.Int52,wo),hn.set(wr.BigInt64,ho),hn.set(wr.Single,po),hn.set(wr.IntPtr,yo),hn.set(wr.Double,bo),hn.set(wr.String,xo),hn.set(wr.Exception,jo),hn.set(wr.JSException,jo),hn.set(wr.JSObject,$o),hn.set(wr.Object,No),hn.set(wr.DateTime,Eo),hn.set(wr.DateTimeOffset,Eo),hn.set(wr.Task,So),hn.set(wr.Action,Ao),hn.set(wr.Function,Ao),hn.set(wr.None,vo),hn.set(wr.Void,vo),hn.set(wr.Discard,vo))}function uo(e,t,n,r,o,s){let i="",a="",c="";const u="converter"+t;let l="null",f="null",_="null",d="null",m=$n(e);if(m===wr.None||m===wr.Void)return{converters:i,call_body:c,marshaler_type:m};const g=Nn(e);if(g!==wr.None){const e=hn.get(g);if(!(e&&"function"===typeof e))throw new Error(`Assert failed: Unknow converter for type ${g} at ${t}`);m!=wr.Nullable?(d="converter"+t+"_res",i+=", "+d,a+=" "+wr[g],s[d]=e):m=g}const w=kn(e);if(w!==wr.None){const e=pn.get(w);if(!(e&&"function"===typeof e))throw new Error(`Assert failed: Unknow converter for type ${w} at ${t}`);l="converter"+t+"_arg1",i+=", "+l,a+=" "+wr[w],s[l]=e}const h=Tn(e);if(h!==wr.None){const e=pn.get(h);if(!(e&&"function"===typeof e))throw new Error(`Assert failed: Unknow converter for type ${h} at ${t}`);f="converter"+t+"_arg2",i+=", "+f,a+=" "+wr[h],s[f]=e}const p=Rn(e);if(p!==wr.None){const e=pn.get(p);if(!(e&&"function"===typeof e))throw new Error(`Assert failed: Unknow converter for type ${p} at ${t}`);_="converter"+t+"_arg3",i+=", "+_,a+=" "+wr[p],s[_]=e}const b=hn.get(m);if(!(b&&"function"===typeof b))throw new Error(`Assert failed: Unknow converter for type ${m} at ${t} `);return i+=", "+u,a+=" "+wr[m],s[u]=b,c=m==wr.Task?`  const ${o} = ${u}(args + ${n}, signature + ${r}, ${d}); // ${a} \n`:m==wr.Action||m==wr.Function?`  const ${o} = ${u}(args + ${n}, signature + ${r}, ${d}, ${l}, ${f}, ${_}); // ${a} \n`:`  const ${o} = ${u}(args + ${n}, signature + ${r}); // ${a} \n`,{converters:i,call_body:c,marshaler_type:m}}function lo(e){const t=undefined;return Dn(e)==wr.None?null:Wn(e)}function fo(e){const t=undefined;return Dn(e)==wr.None?null:Fn(e)}function _o(e){const t=undefined;return Dn(e)==wr.None?null:Bn(e)}function mo(e){const t=undefined;return Dn(e)==wr.None?null:Vn(e)}function go(e){const t=undefined;return Dn(e)==wr.None?null:Hn(e)}function wo(e){const t=undefined;return Dn(e)==wr.None?null:Ln(e)}function ho(e){const t=undefined;return Dn(e)==wr.None?null:Jn(e)}function po(e){const t=undefined;return Dn(e)==wr.None?null:Gn(e)}function bo(e){const t=undefined;return Dn(e)==wr.None?null:Yn(e)}function yo(e){const t=undefined;return Dn(e)==wr.None?null:zn(e)}function vo(){return null}function Eo(e){const t=undefined;return Dn(e)===wr.None?null:qn(e)}function Ao(e,t,n,r,o,s){const i=undefined;if(Dn(e)===wr.None)return null;const a=ur(e);let c=ot(a);return null!==c&&void 0!==c||(c=(e,t,i)=>b.javaScriptExports.call_delegate(a,e,t,i,n,r,o,s),et(c,a)),c}function So(e,t,n){const r=Dn(e);if(r===wr.None)return null;if(r!==wr.Task){if(n||(n=hn.get(r)),!n)throw new Error(`Assert failed: Unknow sub_converter for type ${wr[r]} `);const t=n(e);return new Promise((e=>e(t)))}const o=ar(e);if(0==o)return new Promise((e=>e(void 0)));const s=Ze(o);if(!!!s)throw new Error(`Assert failed: ERR28: promise not found for js_handle: ${o} `);ut(s);const i=at(s),a=i.resolve;return i.resolve=e=>{const t=Dn(e);if(t===wr.None)return a(null),void 0;if(n||(n=hn.get(t)),!n)throw new Error(`Assert failed: Unknow sub_converter for type ${wr[t]}`);const r=n(e);a(r)},s}function Oo(e){const t=On(e,0),n=On(e,1),r=On(e,2),o=On(e,3),s=Dn(t),i=Dn(o),a=ar(r);if(0===a){const{promise:e,promise_control:r}=it(),a=undefined;if(cr(n,Qe(e)),s!==wr.None){const e=jo(t);r.reject(e)}else if(i!==wr.Task){const e=hn.get(i);if(!e)throw new Error(`Assert failed: Unknow sub_converter for type ${wr[i]} `);const t=e(o);r.resolve(t)}}else{const e=Ze(a);if(!!!e)throw new Error(`Assert failed: ERR25: promise not found for js_handle: ${a} `);ut(e);const n=at(e);if(s!==wr.None){const e=jo(t);n.reject(e)}else i!==wr.Task&&n.resolve(o)}Cn(n,wr.Task),Cn(t,wr.None)}function xo(e){const t=undefined;if(Dn(e)==wr.None)return null;const n=fr(e);try{const e=undefined;return xr(n)}finally{n.release()}}function jo(e){const t=Dn(e);if(t==wr.None)return null;if(t==wr.JSException){const t=undefined,n=undefined;return Ze(ar(e))}const n=ur(e);let r=ot(n);if(null===r||void 0===r){const t=xo(e);r=new ManagedError(t),et(r,n)}return r}function $o(e){const t=undefined;if(Dn(e)==wr.None)return null;const n=undefined,r=undefined;return Ze(ar(e))}function No(e){const t=Dn(e);if(t==wr.None)return null;if(t==wr.JSObject){const t=undefined,n=undefined;return Ze(ar(e))}if(t==wr.Array){const t=undefined;return To(e,Un(e))}if(t==wr.Object){const t=ur(e);if(0===t)return null;let n=ot(t);return n||(n=new ManagedObject,et(n,t)),n}const n=hn.get(t);if(!n)throw new Error(`Assert failed: Unknow converter for type ${wr[t]}`);return n(e)}function ko(e,t){if(!!!t)throw new Error("Assert failed: Expected valid sig parameter");const n=undefined;return To(e,kn(t))}function To(e,t){const n=undefined;if(Dn(e)==wr.None)return null;const r=undefined;if(!(-1!=mr(t)))throw new Error(`Assert failed: Element type ${wr[t]} not supported`);const s=zn(e),i=_r(e);let a=null;if(t==wr.String){a=new Array(i);for(let e=0;e<i;e++){const t=On(s,e);a[e]=xo(t)}M.mono_wasm_deregister_root(s)}else if(t==wr.Object){a=new Array(i);for(let e=0;e<i;e++){const t=On(s,e);a[e]=No(t)}M.mono_wasm_deregister_root(s)}else if(t==wr.JSObject){a=new Array(i);for(let e=0;e<i;e++){const t=On(s,e);a[e]=$o(t)}}else if(t==wr.Byte){const e=undefined;a=o.HEAPU8.subarray(s,s+i).slice()}else if(t==wr.Int32){const e=undefined;a=o.HEAP32.subarray(s>>2,(s>>2)+i).slice()}else{if(t!=wr.Double)throw new Error(`NotImplementedException ${wr[t]} `);{const e=undefined;a=o.HEAPF64.subarray(s>>3,(s>>3)+i).slice()}}return o._free(s),a}function Ro(e,t){if(!!!t)throw new Error("Assert failed: Expected valid sig parameter");const n=kn(t),r=zn(e),o=_r(e);let s=null;if(n==wr.Byte)s=new Span(r,o,0);else if(n==wr.Int32)s=new Span(r,o,1);else{if(n!=wr.Double)throw new Error(`NotImplementedException ${wr[n]} `);s=new Span(r,o,2)}return s}function Mo(e,t){if(!!!t)throw new Error("Assert failed: Expected valid sig parameter");const n=kn(t),r=zn(e),o=_r(e);let s=null;if(n==wr.Byte)s=new ArraySegment(r,o,0);else if(n==wr.Int32)s=new ArraySegment(r,o,1);else{if(n!=wr.Double)throw new Error(`NotImplementedException ${wr[n]} `);s=new ArraySegment(r,o,2)}const i=undefined;return et(s,ur(e)),s}let Io,Do;const Uo={};function Co(e){Io=e.mono,Do=e.binding}const Po=Symbol.for("wasm type");function Wo(e){return new Promise((t=>setTimeout(t,e)))}const Fo=it(),Bo=it();let Vo=0,Ho=0,zo=0,Lo=0;const Jo=[],qo=Object.create(null);let Go=0,Yo;const Zo={"js-module-threads":true},Xo={dotnetwasm:true},Qo={"js-module-threads":true,dotnetwasm:true};function Ko(e){var t;const n=null===(t=b.config.assets)||void 0===t?void 0:t.find((t=>t.behavior==e));if(!n)throw new Error(`Assert failed: Can't find asset for ${e}`);return n.resolvedUrl||(n.resolvedUrl=os(n,"")),n}async function es(){b.diagnosticTracing&&console.debug("MONO_WASM: mono_download_assets"),b.maxParallelDownloads=b.config.maxParallelDownloads||b.maxParallelDownloads;try{const e=[];for(const t of b.config.assets){const n=t;if(Qo[n.behavior]||Lo++,!Zo[n.behavior]){const t=Xo[n.behavior];if(zo++,n.pendingDownload){n.pendingDownloadInternal=n.pendingDownload;const r=async()=>{const e=await n.pendingDownloadInternal.response;return t||(n.buffer=await e.arrayBuffer()),++Vo,{asset:n,buffer:n.buffer}};e.push(r())}else{const r=async()=>(n.buffer=await ts(n,!t),{asset:n,buffer:n.buffer});e.push(r())}}}Bo.promise_control.resolve();const t=[];for(const n of e)t.push((async()=>{const e=await n,t=e.asset;if(e.buffer){if(!Qo[t.behavior]){const n=t.pendingDownloadInternal.url,r=new Uint8Array(t.buffer);t.pendingDownloadInternal=null,t.pendingDownload=null,t.buffer=null,e.buffer=null,await lc.promise,is(t,n,r)}}else{const e=undefined;if(Xo[t.behavior])Xo[t.behavior]&&++Vo;else{if(!t.isOptional)throw new Error("Assert failed: Expected asset to have the downloaded buffer");Zo[t.behavior]||zo--,Qo[t.behavior]||Lo--}}})());Promise.all(t).then((()=>{Fo.promise_control.resolve()})).catch((e=>{o.printErr("MONO_WASM: Error in mono_download_assets: "+e),bc(e,true)}))}catch(e){throw o.printErr("MONO_WASM: Error in mono_download_assets: "+e),e}}async function ts(e,t){try{return await ns(e,t)}catch(n){if(c||a)throw n;if(e.pendingDownload&&e.pendingDownloadInternal==e.pendingDownload)throw n;if(e.resolvedUrl&&-1!=e.resolvedUrl.indexOf("file://"))throw n;if(n&&404==n.status)throw n;e.pendingDownloadInternal=void 0,await Bo.promise;try{return await ns(e,t)}catch(n){return e.pendingDownloadInternal=void 0,await Wo(100),await ns(e,t)}}}async function ns(e,t){for(;Yo;)await Yo.promise;try{++Go,Go==b.maxParallelDownloads&&(b.diagnosticTracing&&console.debug("MONO_WASM: Throttling further parallel downloads"),Yo=it());const n=await rs(e);if(!t||!n)return;const r=await n.arrayBuffer();return++Vo,r}finally{if(--Go,Yo&&Go==b.maxParallelDownloads-1){b.diagnosticTracing&&console.debug("MONO_WASM: Resuming more parallel downloads");const e=Yo;Yo=void 0,e.promise_control.resolve()}}}async function rs(e){if(e.buffer){const t=e.buffer;return e.buffer=null,e.pendingDownloadInternal={url:"undefined://"+e.name,name:e.name,response:Promise.resolve({arrayBuffer:()=>t,headers:{get:()=>{}}})},e.pendingDownloadInternal.response}if(e.pendingDownloadInternal&&e.pendingDownloadInternal.response){const t=undefined;return await e.pendingDownloadInternal.response}const t=e.loadRemote&&b.config.remoteSources?b.config.remoteSources:[""];let n;for(let r of t){r=r.trim(),"./"===r&&(r="");const t=os(e,r);e.name===t?b.diagnosticTracing&&console.debug(`MONO_WASM: Attempting to download '${t}'`):b.diagnosticTracing&&console.debug(`MONO_WASM: Attempting to download '${t}' for ${e.name}`);try{const r=ss({name:e.name,resolvedUrl:t,hash:e.hash,behavior:e.behavior});if(e.pendingDownloadInternal=r,n=await r.response,!n.ok)continue;return n}catch(e){continue}}const r=e.isOptional||e.name.match(/\.pdb$/)&&b.config.ignorePdbLoadErrors;if(!n)throw new Error(`Assert failed: Response undefined ${e.name}`);if(r)return o.print(`MONO_WASM: optional download '${n.url}' for ${e.name} failed ${n.status} ${n.statusText}`),void 0;{const t=new Error(`MONO_WASM: download '${n.url}' for ${e.name} failed ${n.status} ${n.statusText}`);throw t.status=n.status,t}}function os(e,t){if(!(null!==t&&void 0!==t))throw new Error(`Assert failed: sourcePrefix must be provided for ${e.name}`);let n;const r=b.config.assemblyRootFolder;if(e.resolvedUrl)n=e.resolvedUrl;else{if(""===t)if("assembly"===e.behavior||"pdb"===e.behavior)n=r?r+"/"+e.name:e.name;else if("resource"===e.behavior){const t=e.culture&&""!==e.culture?`${e.culture}/${e.name}`:e.name;n=r?r+"/"+t:t}else n=e.name;else n=t+e.name;n=b.locateFile(n)}if(!(n&&"string"==typeof n))throw new Error("Assert failed: attemptUrl need to be path or url string");return n}function ss(e){try{if("function"===typeof o.downloadResource){const t=o.downloadResource(e);if(t)return t}const t={};e.hash&&(t.integrity=e.hash);const n=b.fetch_like(e.resolvedUrl,t);return{name:e.name,url:e.resolvedUrl,response:n}}catch(t){const n={ok:false,url:e.resolvedUrl,status:500,statusText:"ERR29: "+t,arrayBuffer:()=>{throw t},json:()=>{throw t}};return{name:e.name,url:e.resolvedUrl,response:Promise.resolve(n)}}}function is(e,t,n){b.diagnosticTracing&&console.debug(`MONO_WASM: Loaded:${e.name} as ${e.behavior} size ${n.length} from ${t}`);const r="string"===typeof e.virtualPath?e.virtualPath:e.name;let s=null;switch(e.behavior){case"dotnetwasm":case"js-module-threads":break;case"resource":case"assembly":case"pdb":Jo.push({url:t,file:r});case"heap":case"icu":s=tn(n),qo[r]=[s,n.length];break;case"vfs":{const e=r.lastIndexOf("/");let t=e>0?r.substr(0,e):null,s=e>0?r.substr(e+1):r;s.startsWith("/")&&(s=s.substr(1)),t?(b.diagnosticTracing&&console.debug(`MONO_WASM: Creating directory '${t}'`),o.FS_createPath("/",t,true,true)):t="/",b.diagnosticTracing&&console.debug(`MONO_WASM: Creating file '${s}' in directory '${t}'`),cs(n,t)||o.FS_createDataFile(t,s,n,true,true,true);break}default:throw new Error(`Unrecognized asset behavior:${e.behavior}, for asset ${e.name}`)}if("assembly"===e.behavior){const e=undefined;if(!M.mono_wasm_add_assembly(r,s,n.length)){const e=Jo.findIndex((e=>e.file==r));Jo.splice(e,1)}}else"icu"===e.behavior?fe(s)||o.printErr(`MONO_WASM: Error loading ICU asset ${e.name}`):"resource"===e.behavior&&M.mono_wasm_add_satellite_assembly(r,e.culture||"",s,n.length);++Ho}async function as(e,t,n){if(!(e&&e.pendingDownloadInternal&&e.pendingDownloadInternal.response))throw new Error("Assert failed: Can't load dotnet.wasm");const r=await e.pendingDownloadInternal.response,o=r.headers&&r.headers.get?r.headers.get("Content-Type"):void 0;let s,i;if("function"===typeof WebAssembly.instantiateStreaming&&"application/wasm"===o){b.diagnosticTracing&&console.debug("MONO_WASM: instantiate_wasm_module streaming");const e=await WebAssembly.instantiateStreaming(r,t);s=e.instance,i=e.module}else{u&&"application/wasm"!==o&&console.warn('MONO_WASM: WebAssembly resource does not have the expected content type "application/wasm", so falling back to slower ArrayBuffer instantiation.');const e=await r.arrayBuffer();b.diagnosticTracing&&console.debug("MONO_WASM: instantiate_wasm_module buffered");const n=await WebAssembly.instantiate(e,t);s=n.instance,i=n.module}n(s,i)}function cs(e,t){if(e.length<8)return false;const n=new DataView(e.buffer),r=undefined;if(1651270004!=n.getUint32(0,true))return false;const s=n.getUint32(4,true);if(0==s||e.length<s+8)return false;let i;try{const t=o.UTF8ArrayToString(e,8,s);if(i=JSON.parse(t),!(i instanceof Array))return false}catch(e){return false}e=e.slice(s+8);const a=new Set;i.filter((e=>{const t=e[0],n=t.lastIndexOf("/"),r=t.slice(0,n+1);a.add(r)})),a.forEach((e=>{o.FS_createPath(t,e,true,true)}));for(const n of i){const r=n[0],s=n[1],i=e.slice(0,s);o.FS_createDataFile(t,r,i,true,true),e=e.slice(s)}return true}async function us(){if(await Fo.promise,b.config.assets){if(!(Vo==zo))throw new Error(`Assert failed: Expected ${zo} assets to be downloaded, but only finished ${Vo}`);if(!(Ho==Lo))throw new Error(`Assert failed: Expected ${Lo} assets to be in memory, but only instantiated ${Ho}`);Jo.forEach((e=>Io.loaded_files.push(e.url))),b.diagnosticTracing&&console.debug("MONO_WASM: all assets are loaded in wasm memory")}}function ls(){return Io.loaded_files}let fs,_s;function ds(e){const t=o;"undefined"===typeof globalThis.performance&&(globalThis.performance=gs),"undefined"===typeof globalThis.URL&&(globalThis.URL=class e{constructor(e){this.url=e}toString(){return this.url}});const n=t.imports=o.imports||{},r=e=>t=>{const n=o.imports[t];return n||e(t)};n.require?b.requirePromise=e.requirePromise=Promise.resolve(r(n.require)):e.require?b.requirePromise=e.requirePromise=Promise.resolve(r(e.require)):e.requirePromise?b.requirePromise=e.requirePromise.then((e=>r(e))):b.requirePromise=e.requirePromise=Promise.resolve(r((e=>{throw new Error(`Please provide Module.imports.${e} or Module.imports.require`)}))),b.scriptDirectory=e.scriptDirectory=bs(e),t.mainScriptUrlOrBlob=e.scriptUrl,t.__locateFile===t.locateFile?t.locateFile=b.locateFile=e=>Es(e)?e:b.scriptDirectory+e:b.locateFile=t.locateFile,n.fetch?e.fetch=b.fetch_like=n.fetch:e.fetch=b.fetch_like=ws,e.noExitRuntime=u;const s=e.updateGlobalBufferAndViews;e.updateGlobalBufferAndViews=e=>{s(e),en(e)}}async function ms(){if(a){if(s.require=await b.requirePromise,globalThis.performance===gs){const{performance:e}=s.require("perf_hooks");globalThis.performance=e}if(globalThis.crypto||(globalThis.crypto={}),!globalThis.crypto.getRandomValues){let e;try{e=s.require("node:crypto")}catch(e){}e?e.webcrypto?globalThis.crypto=e.webcrypto:e.randomBytes&&(globalThis.crypto.getRandomValues=t=>{t&&t.set(e.randomBytes(t.length))}):globalThis.crypto.getRandomValues=()=>{throw new Error("Using node without crypto support. To enable current operation, either provide polyfill for 'globalThis.crypto.getRandomValues' or enable 'node:crypto' module.")}}}}const gs={now:function(){return Date.now()}};async function ws(e,t){try{if(a){if(!fs){const e=await b.requirePromise;_s=e("url"),fs=e("fs")}e.startsWith("file://")&&(e=_s.fileURLToPath(e));const t=await fs.promises.readFile(e);return{ok:true,url:e,arrayBuffer:()=>t,json:()=>JSON.parse(t)}}if("function"===typeof globalThis.fetch)return globalThis.fetch(e,t||{credentials:"same-origin"});if("function"===typeof read){const t=new Uint8Array(read(e,"binary"));return{ok:true,url:e,arrayBuffer:()=>t,json:()=>JSON.parse(o.UTF8ArrayToString(t,0,t.length))}}}catch(t){return{ok:false,url:e,status:500,statusText:"ERR28: "+t,arrayBuffer:()=>{throw t},json:()=>{throw t}}}throw new Error("No fetch implementation available")}function hs(e){return e.replace(/\\/g,"/").replace(/[?#].*/,"")}function ps(e){return e.slice(0,e.lastIndexOf("/"))+"/"}function bs(e){return l&&(e.scriptUrl=self.location.href),e.scriptUrl||(e.scriptUrl="./dotnet.js"),e.scriptUrl=hs(e.scriptUrl),ps(e.scriptUrl)}const ys=/^[a-zA-Z][a-zA-Z\d+\-.]*?:\/\//,vs=/[a-zA-Z]:[\\/]/;function Es(e){return a||c?e.startsWith("/")||e.startsWith("\\")||-1!==e.indexOf("///")||vs.test(e):ys.test(e)}function As(e,t,n,r,o,s){const i=ln(e),a=ln(t),c=ln(s);try{const e=In(n);if(!(1===e))throw new Error(`Assert failed: Signature version ${e} mismatch.`);const t=xr(i),o=xr(a);b.diagnosticTracing&&console.debug(`MONO_WASM: Binding [JSImport] ${t} from ${o}`);const s=xs(t,o),u=Mn(n),l={fn:s,marshal_exception_to_cs:eo,signature:n},f="_bound_js_"+t.replace(/\./g,"_");let _=`//# sourceURL=https://dotnet.generated.invalid/${f} \n`,d="",m="",g="";for(let e=0;e<u;e++){const t=(e+2)*vn,r=(e+2)*En+8,o=`arg${e}`,s=jn(n,e+2),{converters:i,call_body:a}=uo(s,e+2,t,r,o,l);d+=i,m+=a,g+=""===g?o:`, ${o}`}const{converters:w,call_body:h,marshaler_type:p}=Dr(jn(n,1),1,vn,40,"js_result",l);d+=w,_+=`const { signature, fn, marshal_exception_to_cs ${d} } = closure;\n`,_+=`return function ${f} (args) { try {\n`,_+=m,p===wr.Void?(_+=`  const js_result = fn(${g});\n`,_+=`  if (js_result !== undefined) throw new Error('Function ${t} returned unexpected value, C# signature is void');\n`):p===wr.Discard?_+=`  fn(${g});\n`:(_+=`  const js_result = fn(${g});\n`,_+=h);for(let e=0;e<u;e++){const t=jn(n,e+2),r=undefined;if($n(t)==wr.Span){const t=undefined;_+=`  ${`arg${e}`}.dispose();\n`}}_+="} catch (ex) {\n",_+="  marshal_exception_to_cs(args, ex);\n",_+="}}";const y=undefined,v=new Function("closure",_)(l);v[yn]=true;const E=undefined;Mt(r,Qe(v))}catch(e){Us(o,e,c)}finally{c.release(),i.release()}}function Ss(e,t){const n=Ze(e);if(!(n&&"function"===typeof n&&n[yn]))throw new Error(`Assert failed: Bound function handle expected ${e}`);n(t)}function Os(e,t){Ms.set(e,t),b.diagnosticTracing&&console.debug(`MONO_WASM: added module imports '${e}'`)}function xs(e,t){if(!(e&&"string"===typeof e))throw new Error("Assert failed: function_name must be string");let n=i;const r=e.split(".");if(t){if(n=Ms.get(t),!n)throw new Error(`Assert failed: ES6 module ${t} was not imported yet, please call JSHost.Import() first.`)}else"INTERNAL"===r[0]?(n=s,r.shift()):"globalThis"===r[0]&&(n=globalThis,r.shift());for(let t=0;t<r.length-1;t++){const o=r[t],s=n[o];if(!s)throw new Error(`Assert failed: ${o} not found while looking up ${e}`);n=s}const o=undefined,a=n[r[r.length-1]];if(!("function"===typeof a))throw new Error(`Assert failed: ${e} must be a Function but was ${typeof a}`);return a.bind(n)}function js(e,t,n){if(!e)throw new Error("Assert failed: Null reference");e[t]=n}function $s(e,t){if(!e)throw new Error("Assert failed: Null reference");return e[t]}function Ns(e,t){if(!e)throw new Error("Assert failed: Null reference");return t in e}function ks(e,t){if(!e)throw new Error("Assert failed: Null reference");return typeof e[t]}function Ts(){return globalThis}const Rs=new Map,Ms=new Map;function Is(e,t){if(!e)throw new Error("Assert failed: Invalid module_name");if(!t)throw new Error("Assert failed: Invalid module_name");let n=Rs.get(e);const r=!n;return r&&(b.diagnosticTracing&&console.debug(`MONO_WASM: importing ES6 module '${e}' from '${t}'`),n=import(t),Rs.set(e,n)),_t((async()=>{const o=await n;return r&&(Ms.set(e,o),b.diagnosticTracing&&console.debug(`MONO_WASM: imported ES6 module '${e}' from '${t}'`)),o}))}function Ds(e,t){let n="unknown exception";if(t){n=t.toString();const e=t.stack;e&&(e.startsWith(n)?n=e:n+="\n"+e),n=Oe(n)}return e&&o.setValue(e,1,"i32"),n}function Us(e,t,n){const r=undefined;kr(Ds(e,t),n)}const Cs=new Map;function Ps(e,t,n,r,s){const i=ln(e),a=ln(s),c=o;try{const e=In(n);if(!(1===e))throw new Error(`Assert failed: Signature version ${e} mismatch.`);const r=Mn(n),o=xr(i);if(!o)throw new Error("Assert failed: fully_qualified_name must be string");b.diagnosticTracing&&console.debug(`MONO_WASM: Binding [JSExport] ${o}`);const{assembly:s,namespace:u,classname:l,methodname:f}=Hs(o),_=be(s);if(!_)throw new Error("Could not find assembly: "+s);const d=M.mono_wasm_assembly_find_class(_,u,l);if(!d)throw new Error("Could not find class: "+u+":"+l+" in assembly "+s);const m=`__Wrapper_${f}_${t}`,g=M.mono_wasm_assembly_find_method(d,m,-1);if(!g)throw new Error(`Could not find method: ${m} in ${d} [${s}]`);const w={method:g,signature:n,stackSave:c.stackSave,stackRestore:c.stackRestore,alloc_stack_frame:Sn,invoke_method_and_handle_exception:Ws},h="_bound_cs_"+`${u}_${l}_${f}`.replace(/\./g,"_").replace(/\//g,"_");let p=`//# sourceURL=https://dotnet.generated.invalid/${h} \n`,y="",v="";for(let e=0;e<r;e++){const t=(e+2)*vn,r=(e+2)*En+8,o=jn(n,e+2),{converters:s,call_body:i}=Dr(o,e+2,t,r,`arguments[${e}]`,w);v+=s,y+=i}const{converters:E,call_body:A,marshaler_type:S}=uo(jn(n,1),1,vn,40,"js_result",w);v+=E,p+=`const { method, signature, stackSave, stackRestore,  alloc_stack_frame, invoke_method_and_handle_exception ${v} } = closure;\n`,p+=`return function ${h} () {\n`,p+="const sp = stackSave();\n",p+="try {\n",p+=`  const args = alloc_stack_frame(${r+2});\n`,p+=y,p+="  invoke_method_and_handle_exception(method, args);\n",S!==wr.Void&&S!==wr.Discard&&(p+=A),S!==wr.Void&&S!==wr.Discard&&(p+="  return js_result;\n"),p+="} finally {\n",p+="  stackRestore(sp);\n",p+="}}";const O=undefined,x=new Function("closure",p)(w);x[bn]=true,Cs.set(o,x),Bs(s,u,l,f,t,x)}catch(e){o.printErr(e.toString()),Us(r,e,a)}finally{a.release(),i.release()}}function Ws(e,t){const n=M.mono_wasm_invoke_method_bound(e,t);if(n)throw new Error("ERR24: Unexpected error: "+Or(n));if(xn(t)){const e=undefined;throw jo(On(t,0))}}const Fs=new Map;function Bs(e,t,n,r,o,s){const i=`${t}.${n}`.replace(/\//g,".").split(".");let a,c=Fs.get(e);c||(c={},Fs.set(e,c),Fs.set(e+".dll",c)),a=c;for(let e=0;e<i.length;e++){const t=i[e];if(""!=t){let e=a[t];if("undefined"===typeof e&&(e={},a[t]=e),!e)throw new Error(`Assert failed: ${t} not found while looking up ${n}`);a=e}}a[r]||(a[r]=s),a[`${r}.${o}`]=s}async function Vs(e){if(!b.mono_wasm_bindings_is_ready)throw new Error("Assert failed: The runtime must be initialized.");const t=undefined;if(!Fs.get(e)){const t=be(e);if(!t)throw new Error("Could not find assembly: "+e);M.mono_wasm_runtime_run_module_cctor(t)}return Fs.get(e)||{}}function Hs(e){const t=e.substring(e.indexOf("[")+1,e.indexOf("]")).trim(),n=(e=e.substring(e.indexOf("]")+1).trim()).substring(e.indexOf(":")+1);let r="",o=e=e.substring(0,e.indexOf(":")).trim();if(-1!=e.indexOf(".")){const t=e.lastIndexOf(".");r=e.substring(0,t),o=e.substring(t+1)}if(!t.trim())throw new Error("No assembly name specified "+e);if(!o.trim())throw new Error("No class name specified "+e);if(!n.trim())throw new Error("No method name specified "+e);return{assembly:t,namespace:r,classname:o,methodname:n}}function zs(){const e=o,t="System.Runtime.InteropServices.JavaScript";if(b.runtime_interop_module=M.mono_wasm_assembly_load(t),!b.runtime_interop_module)throw"Can't find bindings module assembly: "+t;if(b.runtime_interop_namespace="System.Runtime.InteropServices.JavaScript",b.runtime_interop_exports_classname="JavaScriptExports",b.runtime_interop_exports_class=M.mono_wasm_assembly_find_class(b.runtime_interop_module,b.runtime_interop_namespace,b.runtime_interop_exports_classname),!b.runtime_interop_exports_class)throw"Can't find "+b.runtime_interop_namespace+"."+b.runtime_interop_exports_classname+" class";const n=M.mono_wasm_assembly_find_method(b.runtime_interop_exports_class,"InstallSynchronizationContext",-1),r=Ls("CallEntrypoint");if(!r)throw new Error("Assert failed: Can't find CallEntrypoint method");const s=Ls("ReleaseJSOwnedObjectByGCHandle");if(!s)throw new Error("Assert failed: Can't find ReleaseJSOwnedObjectByGCHandle method");const i=Ls("CreateTaskCallback");if(!i)throw new Error("Assert failed: Can't find CreateTaskCallback method");const a=Ls("CompleteTask");if(!a)throw new Error("Assert failed: Can't find CompleteTask method");const c=Ls("CallDelegate");if(!c)throw new Error("Assert failed: Can't find CallDelegate method");const u=Ls("GetManagedStackTrace");if(!u)throw new Error("Assert failed: Can't find GetManagedStackTrace method");b.javaScriptExports.call_entry_point=(t,n)=>{const o=e.stackSave();try{const s=Sn(4),i=On(s,1),a=On(s,2),c=On(s,3);Lr(a,t),n&&0==n.length&&(n=void 0),oo(c,n,wr.String),Ws(r,s);const u=So(i,void 0,go);return u||Promise.resolve(0)}finally{e.stackRestore(o)}},b.javaScriptExports.release_js_owned_object_by_gc_handle=t=>{if(!t)throw new Error("Assert failed: Must be valid gc_handle");const n=e.stackSave();try{const r=Sn(3),o=On(r,2);Cn(o,wr.Object),lr(o,t),Ws(s,r)}finally{e.stackRestore(n)}},b.javaScriptExports.create_task_callback=()=>{const t=e.stackSave();try{const n=Sn(2);Ws(i,n);const r=undefined;return ur(On(n,1))}finally{e.stackRestore(t)}},b.javaScriptExports.complete_task=(t,n,r,o)=>{const s=e.stackSave();try{const i=Sn(5),c=On(i,2);Cn(c,wr.Object),lr(c,t);const u=On(i,3);if(n)eo(u,n);else{Cn(u,wr.None);const e=On(i,4);if(!o)throw new Error("Assert failed: res_converter missing");o(e,r)}Ws(a,i)}finally{e.stackRestore(s)}},b.javaScriptExports.call_delegate=(t,n,r,o,s,i,a,u)=>{const l=e.stackSave();try{const f=Sn(6),_=On(f,2);if(Cn(_,wr.Object),lr(_,t),i){const e=undefined;i(On(f,3),n)}if(a){const e=undefined;a(On(f,4),r)}if(u){const e=undefined;u(On(f,5),o)}if(Ws(c,f),s){const e=undefined;return s(On(f,1))}}finally{e.stackRestore(l)}},b.javaScriptExports.get_managed_stack_trace=t=>{const n=e.stackSave();try{const r=Sn(3),o=On(r,2);Cn(o,wr.Exception),lr(o,t),Ws(u,r);const s=undefined;return xo(On(r,1))}finally{e.stackRestore(n)}},n&&(b.javaScriptExports.install_synchronization_context=()=>{const t=e.stackSave();try{const r=Sn(2);Ws(n,r)}finally{e.stackRestore(t)}},f||b.javaScriptExports.install_synchronization_context())}function Ls(e){const t=M.mono_wasm_assembly_find_method(b.runtime_interop_exports_class,e,-1);if(!t)throw"Can't find method "+b.runtime_interop_namespace+"."+b.runtime_interop_exports_classname+"."+e;return t}function Js(e,t,n,r,o,s,i){const a=ln(i);try{const s=undefined;Qs(qs(e,t,n,r,o),a,true)}catch(e){Us(s,String(e),a)}finally{a.release()}}function qs(e,t,n,r,o){let s=null;switch(o){case 5:s=new Int8Array(n-t);break;case 6:s=new Uint8Array(n-t);break;case 7:s=new Int16Array(n-t);break;case 8:s=new Uint16Array(n-t);break;case 9:s=new Int32Array(n-t);break;case 10:s=new Uint32Array(n-t);break;case 13:s=new Float32Array(n-t);break;case 14:s=new Float64Array(n-t);break;case 15:s=new Uint8ClampedArray(n-t);break;default:throw new Error("Unknown array type "+o)}return Gs(s,e,t,n,r),s}function Gs(e,t,n,r,s){if(Ys(e)&&e.BYTES_PER_ELEMENT){if(s!==e.BYTES_PER_ELEMENT)throw new Error("Inconsistent element sizes: TypedArray.BYTES_PER_ELEMENT '"+e.BYTES_PER_ELEMENT+"' sizeof managed element: '"+s+"'");let i=(r-n)*s;const a=e.length*e.BYTES_PER_ELEMENT;i>a&&(i=a);const c=undefined,u=n*s;return new Uint8Array(e.buffer,0,i).set(o.HEAPU8.subarray(t+u,t+u+i)),i}throw new Error("Object '"+e+"' is not a typed array")}function Ys(e){return"undefined"!==typeof SharedArrayBuffer?e.buffer instanceof ArrayBuffer||e.buffer instanceof SharedArrayBuffer:e.buffer instanceof ArrayBuffer}function Zs(e,t,n){switch(true){case null===t:case"undefined"===typeof t:return n.clear(),void 0;case"symbol"===typeof t:case"string"===typeof t:return Xi._create_uri_ref(t,n.address),void 0;default:return Ks(e,t,n),void 0}}function Xs(e){const t=fn();try{return Qs(e,t,false),t.value}finally{t.release()}}function Qs(e,t,n){if(T(t))throw new Error("Expected (value, WasmRoot, boolean)");switch(true){case null===e:case"undefined"===typeof e:return t.clear(),void 0;case"number"===typeof e:{let n;return(0|e)===e?(Rt(Uo._box_buffer,e),n=Uo._class_int32):e>>>0===e?($t(Uo._box_buffer,e),n=Uo._class_uint32):(Wt(Uo._box_buffer,e),n=Uo._class_double),M.mono_wasm_box_primitive_ref(n,Uo._box_buffer,8,t.address),void 0}case"string"===typeof e:return kr(e,t),void 0;case"symbol"===typeof e:return Nr(e,t),void 0;case"boolean"===typeof e:return Ot(Uo._box_buffer,e),M.mono_wasm_box_primitive_ref(Uo._class_boolean,Uo._box_buffer,4,t.address),void 0;case true===ft(e):return si(e,t),void 0;case"Date"===e.constructor.name:return Xi._create_date_time_ref(e.getTime(),t.address),void 0;default:return Ks(n,e,t),void 0}}function Ks(e,t,n){if(n.clear(),null!==t&&"undefined"!==typeof t){if(void 0!==t[Ge]){const e=undefined;return Ei(nt(t),n.address),void 0}if(t[Ye]&&(ai(t[Ye],e,n.address),n.value||delete t[Ye]),!n.value){const r=t[Po],o="undefined"===typeof r?0:r,s=Qe(t);Xi._create_cs_owned_proxy_ref(s,o,e?1:0,n.address)}}}function ei(e){const t=e.length*e.BYTES_PER_ELEMENT,n=o._malloc(t),r=new Uint8Array(o.HEAPU8.buffer,n,t);return r.set(new Uint8Array(e.buffer,e.byteOffset,t)),r}function ti(e,t){if(!Ys(e)||!e.BYTES_PER_ELEMENT)throw new Error("Object '"+e+"' is not a typed array");{const n=e[Po],r=ei(e);M.mono_wasm_typed_array_new_ref(r.byteOffset,e.length,e.BYTES_PER_ELEMENT,n,t.address),o._free(r.byteOffset)}}function ni(e){const t=fn();try{return ti(e,t),t.value}finally{t.release()}}function ri(e,t,n){if("number"!==typeof e)throw new Error(`Expected numeric value for enum argument, got '${e}'`);return 0|e}function oi(e,t,n){const r=fn();t?M.mono_wasm_string_array_new_ref(e.length,r.address):M.mono_wasm_obj_array_new_ref(e.length,r.address);const o=fn(0),s=r.address,i=o.address;try{for(let r=0;r<e.length;++r){let a=e[r];t&&(a=a.toString()),Qs(a,o,n),M.mono_wasm_obj_array_set_ref(s,r,i)}return r.value}finally{_n(r,o)}}function si(e,t){if(!e)return t.clear(),null;const n=Qe(e),r=Xi._create_tcs(),o={tcs_gc_handle:r};return et(o,r),e.then((e=>{Xi._set_tcs_result_ref(r,e)}),(e=>{Xi._set_tcs_failure(r,e?e.toString():"")})).finally((()=>{Ke(n),tt(o,r)})),Xi._get_tcs_task_ref(r,t.address),{then_js_handle:n}}function ii(e,t,n){const r=ln(n);try{const n=Ze(e);if(T(n))return Us(t,"ERR06: Invalid JS object handle '"+e+"'",r),void 0;ti(n,r)}catch(e){Us(t,String(e),r)}finally{r.release()}}function ai(e,t,n){if(0===e||e===x)return Rt(n,0),void 0;Xi._get_cs_owned_object_by_js_handle_ref(e,t?1:0,n)}const ci=Symbol.for("wasm delegate_invoke");function ui(e){if(0===e)return;const t=fn(e);try{return di(t)}finally{t.release()}}function li(e){const t=undefined,n=undefined;return Ze(Xi._get_cs_owned_object_js_handle_ref(e.address,0))}function fi(e,t,n,r){switch(t){case 0:return null;case 26:case 27:throw new Error("int64 not available");case 3:case 29:return xr(e);case 4:throw new Error("no idea on how to unbox value types");case 5:return hi(e);case 6:return yi(e);case 7:return vi(e);case 10:case 11:case 12:case 13:case 14:case 15:case 16:case 17:case 18:throw new Error("Marshaling of primitive arrays are not supported.");case 20:return new Date(Xi._get_date_value_ref(e.address));case 21:return Xi._object_to_string_ref(e.address);case 22:return Xi._object_to_string_ref(e.address);case 23:return li(e);case 30:return;default:throw new Error(`no idea on how to unbox object of MarshalType ${t} at offset ${e.value} (root address is ${e.address})`)}}function _i(e,t,n){if(t>=512)throw new Error(`Got marshaling error ${t} when attempting to unbox object at address ${e.value} (root located at ${e.address})`);let r=0;if((4===t||7==t)&&(r=Ht(n),r<1024))throw new Error(`Got invalid MonoType ${r} for object at address ${e.value} (root located at ${e.address})`);return fi(e,t)}function di(e){if(0===e.value)return;const t=Uo._unbox_buffer,n=M.mono_wasm_try_unbox_primitive_and_get_type_ref(e.address,t,Uo._unbox_buffer_size);switch(n){case 1:return Jt(t);case 25:return Ht(t);case 32:return Ht(t);case 24:return Zt(t);case 2:return Xt(t);case 8:return 0!==Jt(t);case 28:return String.fromCharCode(Jt(t));case 0:return null;default:return _i(e,n,t)}}function mi(e){if(0===e)return null;const t=fn(e);try{return wi(t)}finally{t.release()}}function gi(e){return Xi._is_simple_array_ref(e.address)}function wi(e){if(0===e.value)return null;const t=e.address,n=fn(),r=n.address;try{const o=M.mono_wasm_array_length(e.value),s=new Array(o);for(let e=0;e<o;++e)M.mono_wasm_array_get_ref(t,e,r),gi(n)?s[e]=wi(n):s[e]=di(n);return s}finally{n.release()}}function hi(e){if(0===e.value)return null;const t=undefined;return pi(Xi._get_js_owned_object_gc_handle_ref(e.address))}function pi(e){let t=ot(e);if(t)nt(t);else{t=function(...e){nt(t);const n=undefined;return(0,t[ci])(...e)};const n=fn();Ei(e,n.address);try{if("undefined"===typeof t[ci]){const r=M.mono_wasm_get_delegate_invoke_ref(n.address),o=undefined,s=Li(r,Yi(r,n),true);if(t[ci]=s.bind({this_arg_gc_handle:e}),!t[ci])throw new Error("System.Delegate Invoke method can not be resolved.")}}finally{n.release()}et(t,e)}return t}function bi(e,t,n,r){const o=ln(t),s=ln(e),i=ln(r);try{const e=xr(s);if(!e)return Us(n,"Invalid name @"+s.value,i),void 0;const t=globalThis[e];if(null===t||"undefined"===typeof t)return Us(n,"JavaScript host object '"+e+"' not found.",i),void 0;try{const e=wi(o),n=function(e,t){let n=[];n[0]=e,t&&(n=n.concat(t));const r=undefined,o=undefined;return new(e.bind.apply(e,n))},r=n(t,e),s=undefined;Qs(Qe(r),i,false)}catch(e){return Us(n,e,i),void 0}}finally{i.release(),o.release(),s.release()}}function yi(e){if(0===e.value)return null;if(!lt)throw new Error("Promises are not supported thus 'System.Threading.Tasks.Task' can not work in this context.");const t=Xi._get_js_owned_object_gc_handle_ref(e.address);let n=ot(t);if(!n){const r=()=>tt(n,t),{promise:o,promise_control:s}=it(r,r);n=o,Xi._setup_js_cont_ref(e.address,s),et(n,t)}return n}function vi(e){if(0===e.value)return null;const t=Xi._try_get_cs_owned_object_js_handle_ref(e.address,0);if(t){if(t===x)throw new Error("Cannot access a disposed JSObject at "+e.value);return Ze(t)}const n=Xi._get_js_owned_object_gc_handle_ref(e.address);let r=ot(n);return T(r)&&(r=new ManagedObject,et(r,n)),r}function Ei(e,t){if(!e)return Rt(t,0),void 0;Xi._get_js_owned_object_by_gc_handle_ref(e,t)}const Ai=new Map;function Si(e,t,n,r,s,i,a){Et(),o.stackRestore(a),"object"===typeof r&&(r.clear(),null!==t&&null===t.scratchResultRoot?t.scratchResultRoot=r:r.release()),"object"===typeof s&&(s.clear(),null!==t&&null===t.scratchExceptionRoot?t.scratchExceptionRoot=s:s.release()),"object"===typeof i&&(i.clear(),null!==t&&null===t.scratchThisArgRoot?t.scratchThisArgRoot=i:i.release())}function Oi(e,t){if(!b.mono_wasm_bindings_is_ready)throw new Error("Assert failed: The runtime must be initialized.");const n=`${e}-${t}`;let r=Ai.get(n);if(void 0===r){const o=Gi(e);"undefined"===typeof t&&(t=Yi(o,void 0)),r=Li(o,t,false,e),Ai.set(n,r)}return r}function xi(e,t){const n=Me(e);"string"!==typeof t&&(t=Yi(n,void 0));const r=Li(n,t,false,"_"+e+"__entrypoint");return async function(...e){return e.length>0&&Array.isArray(e[0])&&(e[0]=oi(e[0],true,false)),r(...e)}}function ji(e,t,n){if(!b.mono_wasm_bindings_is_ready)throw new Error("Assert failed: The runtime must be initialized.");return t||(t=[[]]),xi(e,n)(...t)}function $i(e,t,n,r,o){const s=ln(n),i=ln(t),a=ln(o);try{const t=xr(i);if(!t||"string"!==typeof t)return Us(r,"ERR12: Invalid method name object @"+i.value,a),void 0;const n=Xe(e);if(T(n))return Us(r,"ERR13: Invalid JS object handle '"+e+"' while invoking '"+t+"'",a),void 0;const o=wi(s);try{const e=n[t];if("undefined"===typeof e)throw new Error("Method: '"+t+"' not found for: '"+Object.prototype.toString.call(n)+"'");const r=undefined;Qs(e.apply(n,o),a,true)}catch(e){Us(r,e,a)}}finally{s.release(),i.release(),a.release()}}function Ni(e,t,n,r){const o=ln(t),s=ln(r);try{const t=xr(o);if(!t)return Us(n,"Invalid property name object '"+o.value+"'",s),void 0;const r=Ze(e);if(T(r))return Us(n,"ERR01: Invalid JS object handle '"+e+"' while geting '"+t+"'",s),void 0;const i=undefined;Qs(r[t],s,true)}catch(e){Us(n,e,s)}finally{s.release(),o.release()}}function ki(e,t,n,r,o,s,i){const a=ln(n),c=ln(t),u=ln(i);try{const n=xr(c);if(!n)return Us(s,"Invalid property name object '"+t+"'",u),void 0;const i=Ze(e);if(T(i))return Us(s,"ERR02: Invalid JS object handle '"+e+"' while setting '"+n+"'",u),void 0;let l=false;const f=di(a);if(r)i[n]=f,l=true;else{if(l=false,!r&&!Object.prototype.hasOwnProperty.call(i,n))return Qs(false,u,false),void 0;true===o?Object.prototype.hasOwnProperty.call(i,n)&&(i[n]=f,l=true):(i[n]=f,l=true)}Qs(l,u,false)}catch(e){Us(s,e,u)}finally{u.release(),c.release(),a.release()}}function Ti(e,t,n,r){const o=ln(r);try{const r=Ze(e);if(T(r))return Us(n,"ERR03: Invalid JS object handle '"+e+"' while getting ["+t+"]",o),void 0;const s=undefined;Qs(r[t],o,true)}catch(e){Us(n,e,o)}finally{o.release()}}function Ri(e,t,n,r,o){const s=ln(n),i=ln(o);try{const n=Ze(e);if(T(n))return Us(r,"ERR04: Invalid JS object handle '"+e+"' while setting ["+t+"]",i),void 0;const o=di(s);n[t]=o,i.clear()}catch(e){Us(r,e,i)}finally{i.release(),s.release()}}function Mi(e,t,n){const r=ln(e),i=ln(n);try{const e=xr(r);let n;if(n=e?"Module"==e?o:"INTERNAL"==e?s:globalThis[e]:globalThis,null===n||void 0===typeof n)return Us(t,"Global object '"+e+"' not found.",i),void 0;Qs(n,i,true)}catch(e){Us(t,e,i)}finally{i.release(),r.release()}}function Ii(e,t,n,r,o){try{const e=globalThis.Blazor;if(!e)throw new Error("The blazor.webassembly.js library is not loaded.");return e._internal.invokeJSFromDotNet(t,n,r,o)}catch(t){const n=t.message+"\n"+t.stack,r=fn();return kr(n,r),r.copy_to_address(e),r.release(),0}}const Di=/[^A-Za-z0-9_$]/g,Ui=new Map,Ci=new Map,Pi=new Map;function Wi(e,t,n,r){let o=null,s=null,i=null;if(r){i=Object.keys(r),s=new Array(i.length);for(let e=0,t=i.length;e<t;e++)s[e]=r[i[e]]}const a=undefined;return o=Fi(e,t,n,i).apply(null,s),o}function Fi(e,t,n,r){const o='"use strict";\r\n';let s="",i="";e?(s="//# sourceURL=https://dotnet.generated.invalid/"+e+"\r\n",i=e):i="unnamed";let a="function "+i+"("+t.join(", ")+") {\r\n"+n+"\r\n};\r\n";const c=/\r(\n?)/g;a=s+o+a.replace(c,"\r\n    ")+`    return ${i};\r\n`;let u=null,l=null;return l=r?r.concat([a]):[a],u=Function.apply(Function,l),u}function Bi(){const e=Ui;e.set("m",{steps:[{}],size:0}),e.set("s",{steps:[{convert_root:kr.bind(Do)}],size:0,needs_root:true}),e.set("S",{steps:[{convert_root:Nr.bind(Do)}],size:0,needs_root:true}),e.set("o",{steps:[{convert_root:Qs.bind(Do)}],size:0,needs_root:true}),e.set("u",{steps:[{convert_root:Zs.bind(Do,false)}],size:0,needs_root:true}),e.set("R",{steps:[{convert_root:Qs.bind(Do),byref:true}],size:0,needs_root:true}),e.set("j",{steps:[{convert:ri.bind(Do),indirect:"i32"}],size:8}),e.set("b",{steps:[{indirect:"bool"}],size:8}),e.set("i",{steps:[{indirect:"i32"}],size:8}),e.set("I",{steps:[{indirect:"u32"}],size:8}),e.set("l",{steps:[{indirect:"i52"}],size:8}),e.set("L",{steps:[{indirect:"u52"}],size:8}),e.set("f",{steps:[{indirect:"float"}],size:8}),e.set("d",{steps:[{indirect:"double"}],size:8})}function Vi(e){const t=[];let n=0,r=false,o=false,s=-1,i=false;for(let a=0;a<e.length;++a){const c=e[a];if(a===e.length-1){if("!"===c){r=true;continue}"m"===c&&(o=true,s=e.length-1)}else if("!"===c)throw new Error("! must be at the end of the signature");const u=Ui.get(c);if(!u)throw new Error("Unknown parameter type "+c);const l=Object.create(u.steps[0]);l.size=u.size,u.needs_root&&(i=true),l.needs_root=u.needs_root,l.key=c,t.push(l),n+=u.size}return{steps:t,size:n,args_marshal:e,is_result_definitely_unmarshaled:r,is_result_possibly_unmarshaled:o,result_unmarshaled_if_argc:s,needs_root_buffer:i}}function Hi(e){let t=Ci.get(e);return t||(t=Vi(e),Ci.set(e,t)),t}function zi(e){const t=Hi(e);if("string"!==typeof t.args_marshal)throw new Error("Corrupt converter for '"+e+"'");if(t.compiled_function&&t.compiled_variadic_function)return t;const n=e.replace("!","_result_unmarshaled");t.name=n;let r=[],s=["method"];const i={Module:o,setI32:Mt,setU32:Nt,setF32:Pt,setF64:Wt,setU52:Ut,setI52:Dt,setB32:Ot,setI32_unchecked:Rt,setU32_unchecked:$t,scratchValueRoot:t.scratchValueRoot,stackAlloc:o.stackAlloc,_zero_region:St};let a=0;const c=8*((4*e.length+7)/8|0),u=t.size+4*e.length+16;r.push("if (!method) throw new Error('no method provided');",`const buffer = stackAlloc(${u});`,`_zero_region(buffer, ${u});`,`const indirectStart = buffer + ${c};`,"");for(let e=0;e<t.steps.length;e++){const n=t.steps[e],c="step"+e,u="value"+e,l="arg"+e,f=`(indirectStart + ${a})`;if(s.push(l),n.convert_root){if(!!n.indirect)throw new Error("Assert failed: converter step cannot both be rooted and indirect");if(!t.scratchValueRoot){const e=o.stackSave();t.scratchValueRoot=ln(e),i.scratchValueRoot=t.scratchValueRoot}i[c]=n.convert_root,r.push(`scratchValueRoot._set_address(${f});`),r.push(`${c}(${l}, scratchValueRoot);`),n.byref?r.push(`let ${u} = ${f};`):r.push(`let ${u} = scratchValueRoot.value;`)}else n.convert?(i[c]=n.convert,r.push(`let ${u} = ${c}(${l}, method, ${e});`)):r.push(`let ${u} = ${l};`);if(n.needs_root&&!n.convert_root&&(r.push("if (!rootBuffer) throw new Error('no root buffer provided');"),r.push(`rootBuffer.set (${e}, ${u});`)),n.indirect){switch(n.indirect){case"bool":r.push(`setB32(${f}, ${u});`);break;case"u32":r.push(`setU32(${f}, ${u});`);break;case"i32":r.push(`setI32(${f}, ${u});`);break;case"float":r.push(`setF32(${f}, ${u});`);break;case"double":r.push(`setF64(${f}, ${u});`);break;case"i52":r.push(`setI52(${f}, ${u});`);break;case"u52":r.push(`setU52(${f}, ${u});`);break;default:throw new Error("Unimplemented indirect type: "+n.indirect)}r.push(`setU32_unchecked(buffer + (${e} * 4), ${f});`),a+=n.size}else r.push(`setU32_unchecked(buffer + (${e} * 4), ${u});`),a+=4;r.push("")}r.push("return buffer;");let l=r.join("\r\n"),f=null,_=null;try{f=Wi("converter_"+n,s,l,i),t.compiled_function=f}catch(e){throw t.compiled_function=null,console.warn("MONO_WASM: compiling converter failed for",l,"with error",e),e}s=["method","args"];const d={converter:f};r=["return converter(","  method,"];for(let e=0;e<t.steps.length;e++)r.push("  args["+e+(e==t.steps.length-1?"]":"], "));r.push(");"),l=r.join("\r\n");try{_=Wi("variadic_converter_"+n,s,l,d),t.compiled_variadic_function=_}catch(e){throw t.compiled_variadic_function=null,console.warn("MONO_WASM: compiling converter failed for",l,"with error",e),e}return t.scratchRootBuffer=null,t.scratchBuffer=0,t}function Li(e,t,n,r){if("string"!==typeof t)throw new Error("args_marshal argument invalid, expected string");const s=`managed_${e}_${t}`;let i=Pi.get(s);if(i)return i;r||(r=s);let a=null;"string"===typeof t&&(a=zi(t));const c=128,u=o._malloc(c),l={method:e,converter:a,scratchRootBuffer:null,scratchBuffer:0,scratchResultRoot:fn(),scratchExceptionRoot:fn(),scratchThisArgRoot:fn()},f={Module:o,mono_wasm_new_root:fn,get_js_owned_object_by_gc_handle_ref:Ei,_create_temp_frame:vt,_handle_exception_for_call:Ji,_teardown_after_call:Si,mono_wasm_try_unbox_primitive_and_get_type_ref:M.mono_wasm_try_unbox_primitive_and_get_type_ref,_unbox_mono_obj_root_with_known_nonprimitive_type:_i,invoke_method_ref:M.mono_wasm_invoke_method_ref,method:e,token:l,unbox_buffer:u,unbox_buffer_size:c,getB32:Ft,getI32:Jt,getU32:Ht,getF32:Zt,getF64:Xt,stackSave:o.stackSave},_=a?"converter_"+a.name:"";a&&(f[_]=a);const d=[],m=["_create_temp_frame();","let resultRoot = token.scratchResultRoot, exceptionRoot = token.scratchExceptionRoot, thisArgRoot = token.scratchThisArgRoot , sp = stackSave();","token.scratchResultRoot = null;","token.scratchExceptionRoot = null;","token.scratchThisArgRoot = null;","if (resultRoot === null)","\tresultRoot = mono_wasm_new_root ();","if (exceptionRoot === null)","\texceptionRoot = mono_wasm_new_root ();","if (thisArgRoot === null)","\tthisArgRoot = mono_wasm_new_root ();",""];if(a){m.push(`let buffer = ${_}.compiled_function(`,"    method,");for(let e=0;e<a.steps.length;e++){const t="arg"+e;d.push(t),m.push("    "+t+(e==a.steps.length-1?"":", "))}m.push(");")}else m.push("let buffer = 0;");if(a&&a.is_result_definitely_unmarshaled?m.push("let is_result_marshaled = false;"):a&&a.is_result_possibly_unmarshaled?m.push(`let is_result_marshaled = arguments.length !== ${a.result_unmarshaled_if_argc};`):m.push("let is_result_marshaled = true;"),m.push("","",""),n?(m.push("get_js_owned_object_by_gc_handle_ref(this.this_arg_gc_handle, thisArgRoot.address);"),m.push("invoke_method_ref (method, thisArgRoot.address, buffer, exceptionRoot.address, resultRoot.address);")):m.push("invoke_method_ref (method, 0, buffer, exceptionRoot.address, resultRoot.address);"),m.push(`_handle_exception_for_call (${_}, token, buffer, resultRoot, exceptionRoot, thisArgRoot, sp);`,"","let resultPtr = resultRoot.value, result = undefined;"),!a)throw new Error("No converter");a.is_result_possibly_unmarshaled&&m.push("if (!is_result_marshaled) "),(a.is_result_definitely_unmarshaled||a.is_result_possibly_unmarshaled)&&m.push("    result = resultPtr;"),a.is_result_definitely_unmarshaled||m.push("if (is_result_marshaled) {","    let resultType = mono_wasm_try_unbox_primitive_and_get_type_ref (resultRoot.address, unbox_buffer, unbox_buffer_size);","    switch (resultType) {","    case 1:","        result = getI32(unbox_buffer); break;","    case 32:","    case 25:","        result = getU32(unbox_buffer); break;","    case 24:","        result = getF32(unbox_buffer); break;","    case 2:","        result = getF64(unbox_buffer); break;","    case 8:","        result = getB32(unbox_buffer); break;","    case 28:","        result = String.fromCharCode(getI32(unbox_buffer)); break;","    case 0:","        result = null; break;","    default:","        result = _unbox_mono_obj_root_with_known_nonprimitive_type (resultRoot, resultType, unbox_buffer); break;","    }","}");let g=r.replace(Di,"_");n&&(g+="_this"),m.push(`_teardown_after_call (${_}, token, buffer, resultRoot, exceptionRoot, thisArgRoot, sp);`,"return result;");const w=undefined;return i=Wi(g,d,m.join("\r\n"),f),Pi.set(s,i),i}function Ji(e,t,n,r,o,s,i){const a=qi(r,o);if(a)throw Si(e,t,n,r,o,s,i),a}function qi(e,t){if(0===t.value)return null;const n=xr(e),r=undefined;return new Error(n)}function Gi(e){const{assembly:t,namespace:n,classname:r,methodname:o}=Hs(e),s=M.mono_wasm_assembly_load(t);if(!s)throw new Error("Could not find assembly: "+t);const i=M.mono_wasm_assembly_find_class(s,n,r);if(!i)throw new Error("Could not find class: "+n+":"+r+" in assembly "+t);const a=M.mono_wasm_assembly_find_method(i,o,-1);if(!a)throw new Error("Could not find method: "+o);return a}function Yi(e,t){return Xi._get_call_sig_ref(e,t?t.address:Uo._null_root.address)}const Zi=[[true,"_get_cs_owned_object_by_js_handle_ref","GetCSOwnedObjectByJSHandleRef","iim"],[true,"_get_cs_owned_object_js_handle_ref","GetCSOwnedObjectJSHandleRef","mi"],[true,"_try_get_cs_owned_object_js_handle_ref","TryGetCSOwnedObjectJSHandleRef","mi"],[false,"_create_cs_owned_proxy_ref","CreateCSOwnedProxyRef","iiim"],[false,"_get_js_owned_object_by_gc_handle_ref","GetJSOwnedObjectByGCHandleRef","im"],[true,"_get_js_owned_object_gc_handle_ref","GetJSOwnedObjectGCHandleRef","m"],[true,"_create_tcs","CreateTaskSource",""],[true,"_set_tcs_result_ref","SetTaskSourceResultRef","iR"],[true,"_set_tcs_failure","SetTaskSourceFailure","is"],[true,"_get_tcs_task_ref","GetTaskSourceTaskRef","im"],[true,"_setup_js_cont_ref","SetupJSContinuationRef","mo"],[true,"_object_to_string_ref","ObjectToStringRef","m"],[true,"_get_date_value_ref","GetDateValueRef","m"],[true,"_create_date_time_ref","CreateDateTimeRef","dm"],[true,"_create_uri_ref","CreateUriRef","sm"],[true,"_is_simple_array_ref","IsSimpleArrayRef","m"],[false,"_get_call_sig_ref","GetCallSignatureRef","im"]],Xi={};function Qi(e,t){const n=undefined;return Li(ea(e),t,false,"BINDINGS_"+e)}function Ki(){Object.prototype[Po]=0,Array.prototype[Po]=1,ArrayBuffer.prototype[Po]=2,DataView.prototype[Po]=3,Function.prototype[Po]=4,Uint8Array.prototype[Po]=11;const e=65536;if(Uo._unbox_buffer_size=65536,Uo._box_buffer=o._malloc(e),Uo._unbox_buffer=o._malloc(Uo._unbox_buffer_size),Uo._class_int32=Ee("System","Int32"),Uo._class_uint32=Ee("System","UInt32"),Uo._class_double=Ee("System","Double"),Uo._class_boolean=Ee("System","Boolean"),Uo._null_root=fn(),Bi(),Uo.runtime_legacy_exports_classname="LegacyExports",Uo.runtime_legacy_exports_class=M.mono_wasm_assembly_find_class(b.runtime_interop_module,b.runtime_interop_namespace,Uo.runtime_legacy_exports_classname),!Uo.runtime_legacy_exports_class)throw"Can't find "+b.runtime_interop_namespace+"."+b.runtime_interop_exports_classname+" class";for(const e of Zi){const t=Xi,[n,r,o,s]=e;if(n)t[r]=function(...e){const n=Qi(o,s);return t[r]=n,n(...e)};else{const e=Qi(o,s);t[r]=e}}}function ea(e){const t=M.mono_wasm_assembly_find_method(Uo.runtime_legacy_exports_class,e,-1);if(!t)throw"Can't find method "+b.runtime_interop_namespace+"."+Uo.runtime_legacy_exports_classname+"."+e;return t}function ta(){return"undefined"!==typeof Response&&"body"in Response.prototype&&"function"===typeof ReadableStream}function na(){return new AbortController}function ra(e){e.abort()}function oa(e){e.__abort_controller.abort(),e.__reader&&e.__reader.cancel().catch((e=>{e&&"AbortError"!==e.name&&o.printErr("MONO_WASM: Error in http_wasm_abort_response: "+e)}))}function sa(e,t,n,r,o,s,i,a){const c=undefined,u=undefined;return ia(e,t,n,r,o,s,new Span(i,a,0).slice())}function ia(e,t,n,r,o,s,i){if(!(e&&"string"===typeof e))throw new Error("Assert failed: expected url string");if(!(t&&n&&Array.isArray(t)&&Array.isArray(n)&&t.length===n.length))throw new Error("Assert failed: expected headerNames and headerValues arrays");if(!(r&&o&&Array.isArray(r)&&Array.isArray(o)&&r.length===o.length))throw new Error("Assert failed: expected headerNames and headerValues arrays");const a=new Headers;for(let e=0;e<t.length;e++)a.append(t[e],n[e]);const c={body:i,headers:a,signal:s.signal};for(let e=0;e<r.length;e++)c[r[e]]=o[e];return _t((async()=>{const t=await fetch(e,c);return t.__abort_controller=s,t}))}function aa(e){if(!e.__headerNames){e.__headerNames=[],e.__headerValues=[];const t=e.headers.entries();for(const n of t)e.__headerNames.push(n[0]),e.__headerValues.push(n[1])}}function ca(e){return aa(e),e.__headerNames}function ua(e){return aa(e),e.__headerValues}function la(e){return _t((async()=>{const t=await e.arrayBuffer();return e.__buffer=t,e.__source_offset=0,t.byteLength}))}function fa(e,t){if(!e.__buffer)throw new Error("Assert failed: expected resoved arrayBuffer");if(e.__source_offset==e.__buffer.byteLength)return 0;const n=new Uint8Array(e.__buffer,e.__source_offset);t.set(n,0);const r=Math.min(t.byteLength,n.byteLength);return e.__source_offset+=r,r}function _a(e,t,n){const r=new Span(t,n,0);return _t((async()=>{if(e.__reader||(e.__reader=e.body.getReader()),e.__chunk||(e.__chunk=await e.__reader.read(),e.__source_offset=0),e.__chunk.done)return 0;const t=e.__chunk.value.byteLength-e.__source_offset;if(!(t>0))throw new Error("Assert failed: expected remaining_source to be greater than 0");const n=Math.min(t,r.byteLength),o=e.__chunk.value.subarray(e.__source_offset,e.__source_offset+n);return r.set(o,0),e.__source_offset+=n,t==n&&(e.__chunk=void 0),n}))}let da=0,ma=false,ga=0,wa;if(globalThis.navigator){const e=globalThis.navigator;e.userAgentData&&e.userAgentData.brands?ma=e.userAgentData.brands.some((e=>"Chromium"==e.brand)):e.userAgent&&(ma=e.userAgent.includes("Chrome"))}function ha(){for(;ga>0;)--ga,M.mono_background_exec()}function pa(){if(!ma)return;const e=(new Date).valueOf(),t=e+36e4,n=undefined,r=1e3;for(let n=Math.max(e+1e3,da);n<t;n+=r){const t=undefined;setTimeout((()=>{M.mono_set_timeout_exec(),ga++,ha()}),n-e)}da=t}function ba(){++ga,setTimeout(ha,0)}function ya(e){function mono_wasm_set_timeout_exec(){M.mono_set_timeout_exec()}wa&&(clearTimeout(wa),wa=void 0),wa=setTimeout(mono_wasm_set_timeout_exec,e)}class va{constructor(){this.queue=[],this.offset=0}getLength(){return this.queue.length-this.offset}isEmpty(){return 0==this.queue.length}enqueue(e){this.queue.push(e)}dequeue(){if(0===this.queue.length)return;const e=this.queue[this.offset];return this.queue[this.offset]=null,2*++this.offset>=this.queue.length&&(this.queue=this.queue.slice(this.offset),this.offset=0),e}peek(){return this.queue.length>0?this.queue[this.offset]:void 0}drain(e){for(;this.getLength();){const t=undefined;e(this.dequeue())}}}const Ea=Symbol.for("wasm ws_pending_send_buffer"),Aa=Symbol.for("wasm ws_pending_send_buffer_offset"),Sa=Symbol.for("wasm ws_pending_send_buffer_type"),Oa=Symbol.for("wasm ws_pending_receive_event_queue"),xa=Symbol.for("wasm ws_pending_receive_promise_queue"),ja=Symbol.for("wasm ws_pending_open_promise"),$a=Symbol.for("wasm ws_pending_close_promises"),Na=Symbol.for("wasm ws_pending_send_promises"),ka=Symbol.for("wasm ws_is_aborted"),Ta=Symbol.for("wasm ws_receive_status_ptr");let Ra=false,Ma,Ia;const Da=65536,Ua=new Uint8Array;function Ca(e,t,n,r){if(!(e&&"string"===typeof e))throw new Error("Assert failed: ERR12: Invalid uri "+typeof e);const o=new globalThis.WebSocket(e,t||void 0),{promise_control:s}=it();o[Oa]=new va,o[xa]=new va,o[ja]=s,o[Na]=[],o[$a]=[],o[Ta]=n,o.binaryType="arraybuffer";const i=()=>{o[ka]||(s.resolve(o),pa())},a=e=>{o[ka]||(za(o,e),pa())},c=e=>{if(o.removeEventListener("message",a),o[ka])return;r&&r(e.code,e.reason),s.reject(e.reason);for(const e of o[$a])e.resolve();const t=undefined;o[xa].drain((e=>{Mt(n,0),Mt(n+4,2),Mt(n+8,1),e.resolve()}))},u=e=>{s.reject(e.message||"WebSocket error")};return o.addEventListener("message",a),o.addEventListener("open",i,{once:true}),o.addEventListener("close",c,{once:true}),o.addEventListener("error",u,{once:true}),o}function Pa(e){if(!!!e)throw new Error("Assert failed: ERR17: expected ws instance");const t=undefined;return e[ja].promise}function Wa(e,t,n,r,s){if(!!!e)throw new Error("Assert failed: ERR17: expected ws instance");const i=undefined,a=Ja(e,new Uint8Array(o.HEAPU8.buffer,t,n),r,s);return s&&a?Ha(e,a):null}function Fa(e,t,n){if(!!!e)throw new Error("Assert failed: ERR18: expected ws instance");const r=e[Oa],o=e[xa],s=e.readyState;if(s!=WebSocket.OPEN&&s!=WebSocket.CLOSING)throw new Error("InvalidState: The WebSocket is not connected.");if(r.getLength()){if(!(0==o.getLength()))throw new Error("Assert failed: ERR20: Invalid WS state");return La(e,r,t,n),null}const{promise:i,promise_control:a}=it(),c=a;return c.buffer_ptr=t,c.buffer_length=n,o.enqueue(c),i}function Ba(e,t,n,r){if(!!!e)throw new Error("Assert failed: ERR19: expected ws instance");if(e.readyState==WebSocket.CLOSED)return null;if(r){const{promise:r,promise_control:o}=it();return e[$a].push(o),"string"===typeof n?e.close(t,n):e.close(t),r}return Ra||(Ra=true,console.warn("WARNING: Web browsers do not support closing the output side of a WebSocket. CloseOutputAsync has closed the socket and discarded any incoming messages.")),"string"===typeof n?e.close(t,n):e.close(t),null}function Va(e){if(!!!e)throw new Error("Assert failed: ERR18: expected ws instance");e[ka]=true;const t=e[ja];t&&t.reject("OperationCanceledException");for(const t of e[$a])t.reject("OperationCanceledException");for(const t of e[Na])t.reject("OperationCanceledException");e[xa].drain((e=>{e.reject("OperationCanceledException")})),e.close(1e3,"Connection was aborted.")}function Ha(e,t){if(e.send(t),e[Ea]=null,e.bufferedAmount<Da)return null;const{promise:n,promise_control:r}=it(),o=e[Na];o.push(r);let s=1;const i=()=>{if(0===e.bufferedAmount)r.resolve();else if(e.readyState!=WebSocket.OPEN)r.reject("InvalidState: The WebSocket is not connected.");else if(!r.isDone)return globalThis.setTimeout(i,s),s=Math.min(1.5*s,1e3),void 0;const t=o.indexOf(r);t>-1&&o.splice(t,1)};return globalThis.setTimeout(i,0),n}function za(e,t){const n=e[Oa],r=e[xa];if("string"===typeof t.data)void 0===Ia&&(Ia=new TextEncoder),n.enqueue({type:0,data:Ia.encode(t.data),offset:0});else{if("ArrayBuffer"!==t.data.constructor.name)throw new Error("ERR19: WebSocket receive expected ArrayBuffer");n.enqueue({type:1,data:new Uint8Array(t.data),offset:0})}if(r.getLength()&&n.getLength()>1)throw new Error("ERR21: Invalid WS state");for(;r.getLength()&&n.getLength();){const t=r.dequeue();La(e,n,t.buffer_ptr,t.buffer_length),t.resolve()}pa()}function La(e,t,n,r){const s=t.peek(),i=Math.min(r,s.data.length-s.offset);if(i>0){const e=s.data.subarray(s.offset,s.offset+i),t=undefined;new Uint8Array(o.HEAPU8.buffer,n,r).set(e,0),s.offset+=i}const a=s.data.length===s.offset?1:0;a&&t.dequeue();const c=e[Ta];Mt(c,i),Mt(c+4,s.type),Mt(c+8,a)}function Ja(e,t,n,r){let o=e[Ea],s=0;const i=t.byteLength;if(o){if(s=e[Aa],n=e[Sa],0!==i){if(s+i>o.length){const n=new Uint8Array(1.5*(s+i+50));n.set(o,0),n.subarray(s).set(t),e[Ea]=o=n}else o.subarray(s).set(t);s+=i,e[Aa]=s}}else r?0!==i&&(o=t,s=i):(0!==i&&(o=t.slice(),s=i,e[Aa]=s,e[Ea]=o),e[Sa]=n);if(r){if(0==s||null==o)return Ua;if(0===n){void 0===Ma&&(Ma=new TextDecoder("utf-8",{fatal:false}));const e="undefined"!==typeof SharedArrayBuffer&&o instanceof SharedArrayBuffer?o.slice(0,s):o.subarray(0,s);return Ma.decode(e)}return o.subarray(0,s)}return null}function qa(){return{mono_wasm_exit:e=>{o.printErr("MONO_WASM: early exit "+e)},mono_wasm_enable_on_demand_gc:M.mono_wasm_enable_on_demand_gc,mono_profiler_init_aot:M.mono_profiler_init_aot,mono_wasm_exec_regression:M.mono_wasm_exec_regression,mono_method_resolve:Gi,mono_intern_string:jr,logging:void 0,mono_wasm_stringify_as_error_with_stack:xe,mono_wasm_get_loaded_files:ls,mono_wasm_send_dbg_command_with_parms:q,mono_wasm_send_dbg_command:G,mono_wasm_get_dbg_command_info:Y,mono_wasm_get_details:ie,mono_wasm_release_object:ce,mono_wasm_call_function_on:oe,mono_wasm_debugger_resume:Z,mono_wasm_detach_debugger:X,mono_wasm_raise_debug_event:K,mono_wasm_change_debugger_log_level:Q,mono_wasm_debugger_attached:te,mono_wasm_runtime_is_ready:b.mono_wasm_runtime_is_ready,get_property:$s,set_property:js,has_property:Ns,get_typeof_property:ks,get_global_this:Ts,get_dotnet_instance:()=>_,dynamic_import:Is,mono_wasm_cancel_promise:dt,ws_wasm_create:Ca,ws_wasm_open:Pa,ws_wasm_send:Wa,ws_wasm_receive:Fa,ws_wasm_close:Ba,ws_wasm_abort:Va,http_wasm_supports_streaming_response:ta,http_wasm_create_abort_controler:na,http_wasm_abort_request:ra,http_wasm_abort_response:oa,http_wasm_fetch:ia,http_wasm_fetch_bytes:sa,http_wasm_get_response_header_names:ca,http_wasm_get_response_header_values:ua,http_wasm_get_response_bytes:fa,http_wasm_get_response_length:la,http_wasm_get_streamed_response_bytes:_a}}function Ga(e){Object.assign(e,{mono_wasm_exit:M.mono_wasm_exit,mono_wasm_enable_on_demand_gc:M.mono_wasm_enable_on_demand_gc,mono_profiler_init_aot:M.mono_profiler_init_aot,mono_wasm_exec_regression:M.mono_wasm_exec_regression})}function Ya(){return{mono_wasm_setenv:xc,mono_wasm_load_bytes_into_heap:tn,mono_wasm_load_icu_data:fe,mono_wasm_runtime_ready:mono_wasm_runtime_ready,mono_wasm_load_data_archive:cs,mono_wasm_load_config:Rc,mono_load_runtime_and_bcl_args:Dc,mono_wasm_new_root_buffer:un,mono_wasm_new_root:fn,mono_wasm_new_external_root:ln,mono_wasm_release_roots:_n,mono_run_main:Re,mono_run_main_and_exit:Te,mono_wasm_add_assembly:null,mono_wasm_load_runtime:kc,config:b.config,loaded_files:[],setB32:Ot,setI8:kt,setI16:Tt,setI32:Mt,setI52:Dt,setU52:Ut,setI64Big:Ct,setU8:xt,setU16:jt,setU32:Nt,setF32:Pt,setF64:Wt,getB32:Ft,getI8:zt,getI16:Lt,getI32:Jt,getI52:qt,getU52:Gt,getI64Big:Yt,getU8:Bt,getU16:Vt,getU32:Ht,getF32:Zt,getF64:Xt}}function Za(e){Object.assign(e,{mono_wasm_add_assembly:M.mono_wasm_add_assembly})}function Xa(){return{bind_static_method:Oi,call_assembly_entry_point:ji,mono_obj_array_new:null,mono_obj_array_set:null,js_string_to_mono_string:Mr,js_typed_array_to_array:ni,mono_array_to_js_array:mi,js_to_mono_obj:Xs,conv_string:Or,unbox_mono_obj:ui,mono_obj_array_new_ref:null,mono_obj_array_set_ref:null,js_string_to_mono_string_root:kr,js_typed_array_to_array_root:ti,js_to_mono_obj_root:Qs,conv_string_root:xr,unbox_mono_obj_root:di,mono_array_root_to_js_array:wi}}function Qa(e){Object.assign(e,{mono_obj_array_new:M.mono_wasm_obj_array_new,mono_obj_array_set:M.mono_wasm_obj_array_set,mono_obj_array_new_ref:M.mono_wasm_obj_array_new_ref,mono_obj_array_set_ref:M.mono_wasm_obj_array_set_ref})}function Ka(){}async function ec(){return console.warn("MONO_WASM: ignoring diagnostics options because this runtime does not support diagnostics"),void 0}let tc,nc=false,rc=false;const oc=it(),sc=it(),ic=it(),ac=it(),cc=it(),uc=it(),lc=it(),fc=it(),_c=it();function dc(e,t){const n=e.instantiateWasm,r=e.preInit?"function"===typeof e.preInit?[e.preInit]:e.preInit:[],o=e.preRun?"function"===typeof e.preRun?[e.preRun]:e.preRun:[],s=e.postRun?"function"===typeof e.postRun?[e.postRun]:e.postRun:[],i=e.onRuntimeInitialized?e.onRuntimeInitialized:()=>{};rc=!e.configSrc&&(!e.config||!e.config.assets||-1==e.config.assets.findIndex((e=>"assembly"===e.behavior))),e.instantiateWasm=(e,t)=>mc(e,t,n),e.preInit=[()=>gc(r)],e.preRun=[()=>wc(o)],e.onRuntimeInitialized=()=>hc(i),e.postRun=[()=>pc(s)],e.ready.then((async()=>{await _c.promise,oc.promise_control.resolve(t)})).catch((e=>{oc.promise_control.reject(e)})),e.ready=oc.promise,e.onAbort||(e.onAbort=()=>Ie)}function mc(e,t,n){if(o.configSrc||o.config||n||o.print("MONO_WASM: configSrc nor config was specified"),tc=o.config?b.config=o.config:b.config=o.config={},b.diagnosticTracing=!!tc.diagnosticTracing,n){const r=undefined;return n(e,((e,n)=>{ic.promise_control.resolve(),t(e,n)}))}return $c(e,t),[]}function gc(e){o.addRunDependency("mono_pre_init");try{yc(),b.diagnosticTracing&&console.debug("MONO_WASM: preInit"),ac.promise_control.resolve(),e.forEach((e=>e()))}catch(e){throw Oc("MONO_WASM: user preInint() failed",e),bc(e,true),e}(async()=>{try{await vc(),rc||await Ec()}catch(e){throw bc(e,true),e}cc.promise_control.resolve(),o.removeRunDependency("mono_pre_init")})()}async function wc(e){o.addRunDependency("mono_pre_run_async"),await ic.promise,await cc.promise,b.diagnosticTracing&&console.debug("MONO_WASM: preRunAsync");try{e.map((e=>e()))}catch(e){throw Oc("MONO_WASM: user callback preRun() failed",e),bc(e,true),e}uc.promise_control.resolve(),o.removeRunDependency("mono_pre_run_async")}async function hc(e){await uc.promise,b.diagnosticTracing&&console.debug("MONO_WASM: onRuntimeInitialized"),lc.promise_control.resolve();try{rc||(await us(),await Ac()),tc.runtimeOptions&&jc(tc.runtimeOptions);try{e()}catch(e){throw Oc("MONO_WASM: user callback onRuntimeInitialized() failed",e),e}await Sc()}catch(e){throw Oc("MONO_WASM: onRuntimeInitializedAsync() failed",e),bc(e,true),e}fc.promise_control.resolve()}async function pc(e){await fc.promise,b.diagnosticTracing&&console.debug("MONO_WASM: postRunAsync");try{e.map((e=>e()))}catch(e){throw Oc("MONO_WASM: user callback posRun() failed",e),bc(e,true),e}_c.promise_control.resolve()}function bc(e,t){b.diagnosticTracing&&console.trace("MONO_WASM: abort_startup"),oc.promise_control.reject(e),ic.promise_control.reject(e),ac.promise_control.reject(e),cc.promise_control.reject(e),uc.promise_control.reject(e),lc.promise_control.reject(e),fc.promise_control.reject(e),_c.promise_control.reject(e),t&&De(1,e)}function yc(){o.addRunDependency("mono_wasm_pre_init_essential"),b.diagnosticTracing&&console.debug("MONO_WASM: mono_wasm_pre_init_essential"),I(),Ga(s),Za(Io),Qa(Do),o.removeRunDependency("mono_wasm_pre_init_essential")}async function vc(){b.diagnosticTracing&&console.debug("MONO_WASM: mono_wasm_pre_init_essential_async"),o.addRunDependency("mono_wasm_pre_init_essential_async"),await ms(),await Rc(o.configSrc),o.removeRunDependency("mono_wasm_pre_init_essential_async")}async function Ec(){b.diagnosticTracing&&console.debug("MONO_WASM: mono_wasm_pre_init_full"),o.addRunDependency("mono_wasm_pre_init_full"),await es(),o.removeRunDependency("mono_wasm_pre_init_full")}async function Ac(){b.diagnosticTracing&&console.debug("MONO_WASM: mono_wasm_before_user_runtime_initialized");try{await Nc(),de(),b.mono_wasm_load_runtime_done||kc("unused",tc.debugLevel),b.mono_wasm_runtime_is_ready||mono_wasm_runtime_ready(),b.mono_wasm_symbols_are_ready||ke("dotnet.js.symbols"),setTimeout((()=>{Ar.init_fields()}))}catch(e){throw Oc("MONO_WASM: Error in mono_wasm_before_user_runtime_initialized",e),e}}async function Sc(){b.diagnosticTracing&&console.debug("MONO_WASM: mono_wasm_after_user_runtime_initialized");try{if(!o.disableDotnet6Compatibility&&o.exports){const e=globalThis;for(let t=0;t<o.exports.length;++t){const n=o.exports[t],r=o[n];void 0!=r?e[n]=r:console.warn(`MONO_WASM: The exported symbol ${n} could not be found in the emscripten module`)}}if(n,b.diagnosticTracing&&console.debug("MONO_WASM: Initializing mono runtime"),o.onDotnetReady)try{await o.onDotnetReady()}catch(e){throw Oc("MONO_WASM: onDotnetReady () failed",e),e}}catch(e){throw Oc("MONO_WASM: Error in mono_wasm_after_user_runtime_initialized",e),e}}function Oc(e,t){o.printErr(`${e}: ${JSON.stringify(t)}`),t.stack&&(o.printErr("MONO_WASM: Stacktrace: \n"),o.printErr(t.stack))}function xc(e,t){M.mono_wasm_setenv(e,t)}function jc(e){if(!Array.isArray(e))throw new Error("Expected runtimeOptions to be an array of strings");const t=o._malloc(4*e.length);let n=0;for(let r=0;r<e.length;++r){const s=e[r];if("string"!==typeof s)throw new Error("Expected runtimeOptions to be an array of strings");o.setValue(t+4*n,M.mono_wasm_strdup(s),"i32"),n+=1}M.mono_wasm_parse_runtime_options(e.length,t)}async function $c(e,t){try{await Rc(o.configSrc),b.diagnosticTracing&&console.debug("MONO_WASM: instantiate_wasm_module");const n=Ko("dotnetwasm");await ts(n,false),await ac.promise,o.addRunDependency("instantiate_wasm_module"),as(n,e,t),b.diagnosticTracing&&console.debug("MONO_WASM: instantiate_wasm_module done"),ic.promise_control.resolve()}catch(e){throw Oc("MONO_WASM: instantiate_wasm_module() failed",e),bc(e,true),e}o.removeRunDependency("instantiate_wasm_module")}async function Nc(){try{const e=undefined;xc("TZ",Intl.DateTimeFormat().resolvedOptions().timeZone||"UTC")}catch(e){xc("TZ","UTC")}for(const e in tc.environmentVariables){const t=tc.environmentVariables[e];if("string"!==typeof t)throw new Error(`Expected environment variable '${e}' to be a string but it was ${typeof t}: '${t}'`);xc(e,t)}tc.runtimeOptions&&jc(tc.runtimeOptions),tc.aotProfilerOptions&&me(tc.aotProfilerOptions),tc.coverageProfilerOptions&&ge(tc.coverageProfilerOptions)}function kc(e,t){if(b.diagnosticTracing&&console.debug("MONO_WASM: mono_wasm_load_runtime"),!b.mono_wasm_load_runtime_done){b.mono_wasm_load_runtime_done=true;try{void 0==t&&(t=0,tc&&tc.debugLevel&&(t=0+t)),M.mono_wasm_load_runtime(e||"unused",t),b.waitForDebugger=tc.waitForDebugger,b.mono_wasm_bindings_is_ready||Tc()}catch(e){if(Oc("MONO_WASM: mono_wasm_load_runtime () failed",e),bc(e,false),c||a){const e=undefined;(0,M.mono_wasm_exit)(1)}throw e}}}function Tc(){if(b.diagnosticTracing&&console.debug("MONO_WASM: bindings_init"),!b.mono_wasm_bindings_is_ready){b.mono_wasm_bindings_is_ready=true;try{zs(),Ki(),co(),Ir(),b._i52_error_scratch_buffer=o._malloc(4)}catch(e){throw Oc("MONO_WASM: Error in bindings_init",e),e}}}async function Rc(e){if(nc)return await sc.promise,void 0;if(nc=true,!e)return t(),sc.promise_control.resolve(),void 0;b.diagnosticTracing&&console.debug("MONO_WASM: mono_wasm_load_config");try{const n=b.locateFile(e),r=await b.fetch_like(n),s=await r.json()||{};if(s.environmentVariables&&"object"!==typeof s.environmentVariables)throw new Error("Expected config.environmentVariables to be unset or a dictionary-style object");if(s.assets=[...s.assets||[],...tc.assets||[]],s.environmentVariables={...s.environmentVariables||{},...tc.environmentVariables||{}},tc=b.config=o.config=Object.assign(o.config,s),t(),o.onConfigLoaded)try{await o.onConfigLoaded(b.config),t()}catch(e){throw Oc("MONO_WASM: onConfigLoaded() failed",e),e}sc.promise_control.resolve()}catch(t){const n=`Failed to load config file ${e} ${t}`;throw bc(n,true),tc=b.config=o.config={message:n,error:t,isError:true},t}function t(){tc.environmentVariables=tc.environmentVariables||{},tc.assets=tc.assets||[],tc.runtimeOptions=tc.runtimeOptions||[],tc.globalizationMode=tc.globalizationMode||"auto",tc.debugLevel,tc.diagnosticTracing,b.diagnosticTracing=!!b.config.diagnosticTracing}}function Mc(e,t,n,r,s){if(true!==b.mono_wasm_runtime_is_ready)return;const i=0!==e?o.UTF8ToString(e).concat(".dll"):"",a=undefined,c=D(new Uint8Array(o.HEAPU8.buffer,t,n));let u;if(r){const e=undefined;u=D(new Uint8Array(o.HEAPU8.buffer,r,s))}K({eventName:"AssemblyLoaded",assembly_name:i,assembly_b64:c,pdb_b64:u})}function Ic(e,t){const n=t.length+1,r=o._malloc(4*n);let s=0;o.setValue(r+4*s,M.mono_wasm_strdup(e),"i32"),s+=1;for(let e=0;e<t.length;++e)o.setValue(r+4*s,M.mono_wasm_strdup(t[e]),"i32"),s+=1;M.mono_wasm_set_main_args(n,r)}async function Dc(e){tc=o.config=b.config=Object.assign(b.config||{},e||{}),await es(),rc||await us()}var Uc,Cc;(function(e){e[e.Sending=0]="Sending",e[e.Closed=1]="Closed",e[e.Error=2]="Error"})(Uc||(Uc={})),function(e){e[e.Idle=0]="Idle",e[e.PartialCommand=1]="PartialCommand",e[e.Error=2]="Error"}(Cc||(Cc={}));const Pc=void 0;function Wc(){return{mono_set_timeout:ya,mono_wasm_asm_loaded:Mc,mono_wasm_fire_debugger_agent_message:mono_wasm_fire_debugger_agent_message,mono_wasm_debugger_log:ue,mono_wasm_add_dbg_command_received:L,schedule_background_exec:ba,mono_wasm_invoke_js_blazor:Ii,mono_wasm_trace_logger:je,mono_wasm_set_entrypoint_breakpoint:ne,mono_wasm_event_pipe_early_startup_callback:Ka,mono_wasm_invoke_js_with_args_ref:$i,mono_wasm_get_object_property_ref:Ni,mono_wasm_set_object_property_ref:ki,mono_wasm_get_by_index_ref:Ti,mono_wasm_set_by_index_ref:Ri,mono_wasm_get_global_object_ref:Mi,mono_wasm_create_cs_owned_object_ref:bi,mono_wasm_release_cs_owned_object:Ke,mono_wasm_typed_array_to_array_ref:ii,mono_wasm_typed_array_from_ref:Js,mono_wasm_bind_js_function:As,mono_wasm_invoke_bound_function:Ss,mono_wasm_bind_cs_function:Ps,mono_wasm_marshal_promise:Oo,mono_wasm_load_icu_data:fe,mono_wasm_get_icudt_name:_e,...Pc}}class Fc{constructor(){this.moduleConfig={disableDotnet6Compatibility:true,configSrc:"./mono-config.json",config:b.config}}withModuleConfig(e){try{return Object.assign(this.moduleConfig,e),this}catch(e){throw De(1,e),e}}withConsoleForwarding(){try{const e={forwardConsoleLogsToWS:true};return Object.assign(this.moduleConfig.config,e),this}catch(e){throw De(1,e),e}}withAsyncFlushOnExit(){try{const e={asyncFlushOnExit:true};return Object.assign(this.moduleConfig.config,e),this}catch(e){throw De(1,e),e}}withExitCodeLogging(){try{const e={logExitCode:true};return Object.assign(this.moduleConfig.config,e),this}catch(e){throw De(1,e),e}}withElementOnExit(){try{const e={appendElementOnExit:true};return Object.assign(this.moduleConfig.config,e),this}catch(e){throw De(1,e),e}}withWaitingForDebugger(e){try{const t={waitForDebugger:e};return Object.assign(this.moduleConfig.config,t),this}catch(e){throw De(1,e),e}}withConfig(e){try{const t={...e};return t.assets=[...this.moduleConfig.config.assets||[],...t.assets||[]],t.environmentVariables={...this.moduleConfig.config.environmentVariables||{},...t.environmentVariables||{}},Object.assign(this.moduleConfig.config,t),this}catch(e){throw De(1,e),e}}withConfigSrc(e){try{if(!(e&&"string"===typeof e))throw new Error("Assert failed: must be file path or URL");return Object.assign(this.moduleConfig,{configSrc:e}),this}catch(e){throw De(1,e),e}}withVirtualWorkingDirectory(e){try{if(!(e&&"string"===typeof e))throw new Error("Assert failed: must be directory path");return this.virtualWorkingDirectory=e,this}catch(e){throw De(1,e),e}}withEnvironmentVariable(e,t){try{return this.moduleConfig.config.environmentVariables[e]=t,this}catch(e){throw De(1,e),e}}withEnvironmentVariables(e){try{if(!(e&&"object"===typeof e))throw new Error("Assert failed: must be dictionary object");return Object.assign(this.moduleConfig.config.environmentVariables,e),this}catch(e){throw De(1,e),e}}withDiagnosticTracing(e){try{if(!("boolean"===typeof e))throw new Error("Assert failed: must be boolean");return this.moduleConfig.config.diagnosticTracing=e,this}catch(e){throw De(1,e),e}}withDebugging(e){try{if(!(e&&"number"===typeof e))throw new Error("Assert failed: must be number");return this.moduleConfig.config.debugLevel=e,this}catch(e){throw De(1,e),e}}withApplicationArguments(...e){try{if(!(e&&Array.isArray(e)))throw new Error("Assert failed: must be array of strings");return this.applicationArguments=e,this}catch(e){throw De(1,e),e}}withRuntimeOptions(e){try{if(!(e&&Array.isArray(e)))throw new Error("Assert failed: must be array of strings");return Object.assign(this.moduleConfig,{runtimeOptions:e}),this}catch(e){throw De(1,e),e}}withMainAssembly(e){try{return this.moduleConfig.config.mainAssemblyName=e,this}catch(e){throw De(1,e),e}}withApplicationArgumentsFromQuery(){try{if("undefined"!=typeof globalThis.URLSearchParams){const e=undefined,t=new URLSearchParams(window.location.search).getAll("arg");return this.withApplicationArguments(...t)}throw new Error("URLSearchParams is supported")}catch(e){throw De(1,e),e}}async create(){try{if(!this.instance){if(u&&!f&&this.moduleConfig.config.forwardConsoleLogsToWS&&"undefined"!=typeof globalThis.WebSocket&&Ne("main",globalThis.console,globalThis.location.origin),a){const e=await import("process");if(e.versions.node.split(".")[0]<14)throw new Error(`NodeJS at '${e.execPath}' has too low version '${e.versions.node}'`)}if(!this.moduleConfig)throw new Error("Assert failed: Null moduleConfig");if(!this.moduleConfig.config)throw new Error("Assert failed: Null moduleConfig.config");this.instance=await m(this.moduleConfig)}if(this.virtualWorkingDirectory){const e=this.instance.Module.FS,t=e.stat(this.virtualWorkingDirectory);if(!(t&&e.isDir(t.mode)))throw new Error(`Assert failed: Could not find working directory ${this.virtualWorkingDirectory}`);e.chdir(this.virtualWorkingDirectory)}return this.instance}catch(e){throw De(1,e),e}}async run(){try{if(!this.moduleConfig.config)throw new Error("Assert failed: Null moduleConfig.config");if(this.instance||await this.create(),!this.moduleConfig.config.mainAssemblyName)throw new Error("Assert failed: Null moduleConfig.config.mainAssemblyName");if(!this.applicationArguments)if(a){const e=await import("process");this.applicationArguments=e.argv.slice(2)}else this.applicationArguments=[];return this.instance.runMainAndExit(this.moduleConfig.config.mainAssemblyName,this.applicationArguments)}catch(e){throw De(1,e),e}}}const Bc=new Fc;function Vc(){const e=undefined;return{runMain:Re,runMainAndExit:Te,setEnvironmentVariable:xc,getAssemblyExports:Vs,setModuleImports:Os,getConfig:()=>b.config,setHeapB32:Ot,setHeapU8:xt,setHeapU16:jt,setHeapU32:Nt,setHeapI8:kt,setHeapI16:Tt,setHeapI32:Mt,setHeapI52:Dt,setHeapU52:Ut,setHeapI64Big:Ct,setHeapF32:Pt,setHeapF64:Wt,getHeapB32:Ft,getHeapU8:Bt,getHeapU16:Vt,getHeapU32:Ht,getHeapI8:zt,getHeapI16:Lt,getHeapI32:Jt,getHeapI52:qt,getHeapU52:Gt,getHeapI64Big:Yt,getHeapF32:Zt,getHeapF64:Xt}}function Hc(){const e=undefined;return{dotnet:Bc,exit:De}}const zc=Jc,Lc=Gc;function Jc(n,o,s,i){const a=o.module,c=globalThis;g(n,o),Co(o),ds(s),Object.assign(o.mono,Ya()),Object.assign(o.binding,Xa()),Object.assign(o.internal,qa()),Object.assign(o.internal,qa());const u=Vc();if(e.__linker_exports=Wc(),Object.assign(_,{MONO:o.mono,BINDING:o.binding,INTERNAL:o.internal,IMPORTS:o.marshaled_imports,Module:a,runtimeBuildInfo:{productVersion:t,buildConfiguration:r},...u}),Object.assign(i,u),o.module.__undefinedConfig&&(a.disableDotnet6Compatibility=true,a.configSrc="./mono-config.json"),a.print||(a.print=console.log.bind(console)),a.printErr||(a.printErr=console.error.bind(console)),"undefined"===typeof a.disableDotnet6Compatibility&&(a.disableDotnet6Compatibility=true),n.isGlobal||!a.disableDotnet6Compatibility){Object.assign(a,_),a.mono_bind_static_method=(e,t)=>(console.warn("MONO_WASM: Module.mono_bind_static_method is obsolete, please use [JSExportAttribute] interop instead"),Oi(e,t));const e=(e,t)=>{if("undefined"!==typeof c[e])return;let n;Object.defineProperty(globalThis,e,{get:()=>{if(T(n)){const r=(new Error).stack,o=r?r.substr(r.indexOf("\n",8)+1):"";console.warn(`MONO_WASM: global ${e} is obsolete, please use Module.${e} instead ${o}`),n=t()}return n}})};c.MONO=o.mono,c.BINDING=o.binding,c.INTERNAL=o.internal,n.isGlobal||(c.Module=a),e("cwrap",(()=>a.cwrap)),e("addRunDependency",(()=>a.addRunDependency)),e("removeRunDependency",(()=>a.removeRunDependency))}let l;return c.getDotnetRuntime?l=c.getDotnetRuntime.__list:(c.getDotnetRuntime=e=>c.getDotnetRuntime.__list.getRuntime(e),c.getDotnetRuntime.__list=l=new qc),l.registerRuntime(_),dc(a,_),_}e.__linker_exports=null;class qc{constructor(){this.list={}}registerRuntime(e){return e.runtimeId=Object.keys(this.list).length,this.list[e.runtimeId]=Be(e),e.runtimeId}getRuntime(e){const t=this.list[e];return t?t.deref():void 0}}function Gc(e,t){w(t),Object.assign(d,Hc()),h(e)}return e.__initializeImportsAndExports=zc,e.__setEmscriptenEntrypoint=Lc,e.moduleExports=d,Object.defineProperty(e,"__esModule",{value:true}),e}({});

var createDotnetRuntime = (() => {
  var _scriptDir = import.meta.url;
  
  return (
function(createDotnetRuntime) {
  createDotnetRuntime = createDotnetRuntime || {};



"use strict";

// The Module object: Our interface to the outside world. We import
// and export values on it. There are various ways Module can be used:
// 1. Not defined. We create it here
// 2. A function parameter, function(Module) { ..generated code.. }
// 3. pre-run appended it, var Module = {}; ..generated code..
// 4. External script tag defines var Module.
// We need to check if Module already exists (e.g. case 3 above).
// Substitution will be replaced with actual code on later stage of the build,
// this way Closure Compiler will not mangle it (e.g. case 4. above).
// Note that if you want to run closure, and also to use Module
// after the generated code, you will need to define   var Module = {};
// before the code. Then that object will be used in the code, and you
// can continue to use Module afterwards as well.
var Module = typeof createDotnetRuntime != 'undefined' ? createDotnetRuntime : {};

// See https://caniuse.com/mdn-javascript_builtins_object_assign

// Set up the promise that indicates the Module is initialized
var readyPromiseResolve, readyPromiseReject;
Module['ready'] = new Promise(function(resolve, reject) {
  readyPromiseResolve = resolve;
  readyPromiseReject = reject;
});

// --pre-jses are emitted after the Module integration code, so that they can
// refer to Module (if they choose; they can also define Module)
let ENVIRONMENT_IS_GLOBAL = false;
var require = require || undefined;
var __dirname = __dirname || '';
var __callbackAPI = { MONO, BINDING, INTERNAL, IMPORTS };
if (typeof createDotnetRuntime === "function") {
    __callbackAPI.Module = Module = { ready: Module.ready };
    const extension = createDotnetRuntime(__callbackAPI)
    if (extension.ready) {
        throw new Error("MONO_WASM: Module.ready couldn't be redefined.")
    }
    Object.assign(Module, extension);
    createDotnetRuntime = Module;
    if (!createDotnetRuntime.locateFile) createDotnetRuntime.locateFile = createDotnetRuntime.__locateFile = (path) => scriptDirectory + path;
}
else if (typeof createDotnetRuntime === "object") {
    __callbackAPI.Module = Module = { ready: Module.ready, __undefinedConfig: Object.keys(createDotnetRuntime).length === 1 };
    Object.assign(Module, createDotnetRuntime);
    createDotnetRuntime = Module;
    if (!createDotnetRuntime.locateFile) createDotnetRuntime.locateFile = createDotnetRuntime.__locateFile = (path) => scriptDirectory + path;
}
else {
    throw new Error("MONO_WASM: Can't use moduleFactory callback of createDotnetRuntime function.")
}

// Sometimes an existing Module object exists with properties
// meant to overwrite the default module functionality. Here
// we collect those properties and reapply _after_ we configure
// the current environment's defaults to avoid having to be so
// defensive during initialization.
var moduleOverrides = Object.assign({}, Module);

var arguments_ = [];
var thisProgram = './this.program';
var quit_ = (status, toThrow) => {
  throw toThrow;
};

// Determine the runtime environment we are in. You can customize this by
// setting the ENVIRONMENT setting at compile time (see settings.js).

// Attempt to auto-detect the environment
var ENVIRONMENT_IS_WEB = typeof window == 'object';
var ENVIRONMENT_IS_WORKER = typeof importScripts == 'function';
// N.b. Electron.js environment is simultaneously a NODE-environment, but
// also a web environment.
var ENVIRONMENT_IS_NODE = typeof process == 'object' && typeof process.versions == 'object' && typeof process.versions.node == 'string';
var ENVIRONMENT_IS_SHELL = !ENVIRONMENT_IS_WEB && !ENVIRONMENT_IS_NODE && !ENVIRONMENT_IS_WORKER;

// `/` should be present at the end if `scriptDirectory` is not empty
var scriptDirectory = '';
function locateFile(path) {
  if (Module['locateFile']) {
    return Module['locateFile'](path, scriptDirectory);
  }
  return scriptDirectory + path;
}

// Hooks that are implemented differently in different runtime environments.
var read_,
    readAsync,
    readBinary,
    setWindowTitle;

// Normally we don't log exceptions but instead let them bubble out the top
// level where the embedding environment (e.g. the browser) can handle
// them.
// However under v8 and node we sometimes exit the process direcly in which case
// its up to use us to log the exception before exiting.
// If we fix https://github.com/emscripten-core/emscripten/issues/15080
// this may no longer be needed under node.
function logExceptionOnExit(e) {
  if (e instanceof ExitStatus) return;
  let toLog = e;
  err('exiting due to exception: ' + toLog);
}

var fs;
var nodePath;
var requireNodeFS;

if (ENVIRONMENT_IS_NODE) {
  if (ENVIRONMENT_IS_WORKER) {
    scriptDirectory = require('path').dirname(scriptDirectory) + '/';
  } else {
    scriptDirectory = __dirname + '/';
  }

// include: node_shell_read.js


requireNodeFS = () => {
  // Use nodePath as the indicator for these not being initialized,
  // since in some environments a global fs may have already been
  // created.
  if (!nodePath) {
    fs = require('fs');
    nodePath = require('path');
  }
};

read_ = function shell_read(filename, binary) {
  requireNodeFS();
  filename = nodePath['normalize'](filename);
  return fs.readFileSync(filename, binary ? undefined : 'utf8');
};

readBinary = (filename) => {
  var ret = read_(filename, true);
  if (!ret.buffer) {
    ret = new Uint8Array(ret);
  }
  return ret;
};

readAsync = (filename, onload, onerror) => {
  requireNodeFS();
  filename = nodePath['normalize'](filename);
  fs.readFile(filename, function(err, data) {
    if (err) onerror(err);
    else onload(data.buffer);
  });
};

// end include: node_shell_read.js
  if (process['argv'].length > 1) {
    thisProgram = process['argv'][1].replace(/\\/g, '/');
  }

  arguments_ = process['argv'].slice(2);

  // MODULARIZE will export the module in the proper place outside, we don't need to export here

  process['on']('uncaughtException', function(ex) {
    // suppress ExitStatus exceptions from showing an error
    if (!(ex instanceof ExitStatus)) {
      throw ex;
    }
  });

  // Without this older versions of node (< v15) will log unhandled rejections
  // but return 0, which is not normally the desired behaviour.  This is
  // not be needed with node v15 and about because it is now the default
  // behaviour:
  // See https://nodejs.org/api/cli.html#cli_unhandled_rejections_mode
  process['on']('unhandledRejection', function(reason) { throw reason; });

  quit_ = (status, toThrow) => {
    if (keepRuntimeAlive()) {
      process['exitCode'] = status;
      throw toThrow;
    }
    logExceptionOnExit(toThrow);
    process['exit'](status);
  };

  Module['inspect'] = function () { return '[Emscripten Module object]'; };

} else
if (ENVIRONMENT_IS_SHELL) {

  if (typeof read != 'undefined') {
    read_ = function shell_read(f) {
      return read(f);
    };
  }

  readBinary = function readBinary(f) {
    let data;
    if (typeof readbuffer == 'function') {
      return new Uint8Array(readbuffer(f));
    }
    data = read(f, 'binary');
    assert(typeof data == 'object');
    return data;
  };

  readAsync = function readAsync(f, onload, onerror) {
    setTimeout(() => onload(readBinary(f)), 0);
  };

  if (typeof scriptArgs != 'undefined') {
    arguments_ = scriptArgs;
  } else if (typeof arguments != 'undefined') {
    arguments_ = arguments;
  }

  if (typeof quit == 'function') {
    quit_ = (status, toThrow) => {
      logExceptionOnExit(toThrow);
      quit(status);
    };
  }

  if (typeof print != 'undefined') {
    // Prefer to use print/printErr where they exist, as they usually work better.
    if (typeof console == 'undefined') console = /** @type{!Console} */({});
    console.log = /** @type{!function(this:Console, ...*): undefined} */ (print);
    console.warn = console.error = /** @type{!function(this:Console, ...*): undefined} */ (typeof printErr != 'undefined' ? printErr : print);
  }

} else

// Note that this includes Node.js workers when relevant (pthreads is enabled).
// Node.js workers are detected as a combination of ENVIRONMENT_IS_WORKER and
// ENVIRONMENT_IS_NODE.
if (ENVIRONMENT_IS_WEB || ENVIRONMENT_IS_WORKER) {
  if (ENVIRONMENT_IS_WORKER) { // Check worker, not web, since window could be polyfilled
    scriptDirectory = self.location.href;
  } else if (typeof document != 'undefined' && document.currentScript) { // web
    scriptDirectory = document.currentScript.src;
  }
  // When MODULARIZE, this JS may be executed later, after document.currentScript
  // is gone, so we saved it, and we use it here instead of any other info.
  if (_scriptDir) {
    scriptDirectory = _scriptDir;
  }
  // blob urls look like blob:http://site.com/etc/etc and we cannot infer anything from them.
  // otherwise, slice off the final part of the url to find the script directory.
  // if scriptDirectory does not contain a slash, lastIndexOf will return -1,
  // and scriptDirectory will correctly be replaced with an empty string.
  // If scriptDirectory contains a query (starting with ?) or a fragment (starting with #),
  // they are removed because they could contain a slash.
  if (scriptDirectory.indexOf('blob:') !== 0) {
    scriptDirectory = scriptDirectory.substr(0, scriptDirectory.replace(/[?#].*/, "").lastIndexOf('/')+1);
  } else {
    scriptDirectory = '';
  }

  // Differentiate the Web Worker from the Node Worker case, as reading must
  // be done differently.
  {
// include: web_or_worker_shell_read.js


  read_ = (url) => {
      var xhr = new XMLHttpRequest();
      xhr.open('GET', url, false);
      xhr.send(null);
      return xhr.responseText;
  }

  if (ENVIRONMENT_IS_WORKER) {
    readBinary = (url) => {
        var xhr = new XMLHttpRequest();
        xhr.open('GET', url, false);
        xhr.responseType = 'arraybuffer';
        xhr.send(null);
        return new Uint8Array(/** @type{!ArrayBuffer} */(xhr.response));
    };
  }

  readAsync = (url, onload, onerror) => {
    var xhr = new XMLHttpRequest();
    xhr.open('GET', url, true);
    xhr.responseType = 'arraybuffer';
    xhr.onload = () => {
      if (xhr.status == 200 || (xhr.status == 0 && xhr.response)) { // file URLs can return 0
        onload(xhr.response);
        return;
      }
      onerror();
    };
    xhr.onerror = onerror;
    xhr.send(null);
  }

// end include: web_or_worker_shell_read.js
  }

  setWindowTitle = (title) => document.title = title;
} else
{
}

var out = Module['print'] || console.log.bind(console);
var err = Module['printErr'] || console.warn.bind(console);

// Merge back in the overrides
Object.assign(Module, moduleOverrides);
// Free the object hierarchy contained in the overrides, this lets the GC
// reclaim data used e.g. in memoryInitializerRequest, which is a large typed array.
moduleOverrides = null;

// Emit code to handle expected values on the Module object. This applies Module.x
// to the proper local x. This has two benefits: first, we only emit it if it is
// expected to arrive, and second, by using a local everywhere else that can be
// minified.

if (Module['arguments']) arguments_ = Module['arguments'];

if (Module['thisProgram']) thisProgram = Module['thisProgram'];

if (Module['quit']) quit_ = Module['quit'];

// perform assertions in shell.js after we set up out() and err(), as otherwise if an assertion fails it cannot print the message




var STACK_ALIGN = 16;
var POINTER_SIZE = 4;

function getNativeTypeSize(type) {
  switch (type) {
    case 'i1': case 'i8': case 'u8': return 1;
    case 'i16': case 'u16': return 2;
    case 'i32': case 'u32': return 4;
    case 'i64': case 'u64': return 8;
    case 'float': return 4;
    case 'double': return 8;
    default: {
      if (type[type.length - 1] === '*') {
        return POINTER_SIZE;
      } else if (type[0] === 'i') {
        const bits = Number(type.substr(1));
        assert(bits % 8 === 0, 'getNativeTypeSize invalid bits ' + bits + ', type ' + type);
        return bits / 8;
      } else {
        return 0;
      }
    }
  }
}

function warnOnce(text) {
  if (!warnOnce.shown) warnOnce.shown = {};
  if (!warnOnce.shown[text]) {
    warnOnce.shown[text] = 1;
    err(text);
  }
}

// include: runtime_functions.js


// This gives correct answers for everything less than 2^{14} = 16384
// I hope nobody is contemplating functions with 16384 arguments...
function uleb128Encode(n) {
  if (n < 128) {
    return [n];
  }
  return [(n % 128) | 128, n >> 7];
}

// Wraps a JS function as a wasm function with a given signature.
function convertJsFunctionToWasm(func, sig) {

  // If the type reflection proposal is available, use the new
  // "WebAssembly.Function" constructor.
  // Otherwise, construct a minimal wasm module importing the JS function and
  // re-exporting it.
  if (typeof WebAssembly.Function == "function") {
    var typeNames = {
      'i': 'i32',
      'j': 'i64',
      'f': 'f32',
      'd': 'f64',
      'p': 'i32',
    };
    var type = {
      parameters: [],
      results: sig[0] == 'v' ? [] : [typeNames[sig[0]]]
    };
    for (var i = 1; i < sig.length; ++i) {
      type.parameters.push(typeNames[sig[i]]);
    }
    return new WebAssembly.Function(type, func);
  }

  // The module is static, with the exception of the type section, which is
  // generated based on the signature passed in.
  var typeSection = [
    0x01, // count: 1
    0x60, // form: func
  ];
  var sigRet = sig.slice(0, 1);
  var sigParam = sig.slice(1);
  var typeCodes = {
    'i': 0x7f, // i32
    'p': 0x7f, // i32
    'j': 0x7e, // i64
    'f': 0x7d, // f32
    'd': 0x7c, // f64
  };

  // Parameters, length + signatures
  typeSection = typeSection.concat(uleb128Encode(sigParam.length));
  for (var i = 0; i < sigParam.length; ++i) {
    typeSection.push(typeCodes[sigParam[i]]);
  }

  // Return values, length + signatures
  // With no multi-return in MVP, either 0 (void) or 1 (anything else)
  if (sigRet == 'v') {
    typeSection.push(0x00);
  } else {
    typeSection = typeSection.concat([0x01, typeCodes[sigRet]]);
  }

  // Write the section code and overall length of the type section into the
  // section header
  typeSection = [0x01 /* Type section code */].concat(
    uleb128Encode(typeSection.length),
    typeSection
  );

  // Rest of the module is static
  var bytes = new Uint8Array([
    0x00, 0x61, 0x73, 0x6d, // magic ("\0asm")
    0x01, 0x00, 0x00, 0x00, // version: 1
  ].concat(typeSection, [
    0x02, 0x07, // import section
      // (import "e" "f" (func 0 (type 0)))
      0x01, 0x01, 0x65, 0x01, 0x66, 0x00, 0x00,
    0x07, 0x05, // export section
      // (export "f" (func 0 (type 0)))
      0x01, 0x01, 0x66, 0x00, 0x00,
  ]));

   // We can compile this wasm module synchronously because it is very small.
  // This accepts an import (at "e.f"), that it reroutes to an export (at "f")
  var module = new WebAssembly.Module(bytes);
  var instance = new WebAssembly.Instance(module, {
    'e': {
      'f': func
    }
  });
  var wrappedFunc = instance.exports['f'];
  return wrappedFunc;
}

var freeTableIndexes = [];

// Weak map of functions in the table to their indexes, created on first use.
var functionsInTableMap;

function getEmptyTableSlot() {
  // Reuse a free index if there is one, otherwise grow.
  if (freeTableIndexes.length) {
    return freeTableIndexes.pop();
  }
  // Grow the table
  try {
    wasmTable.grow(1);
  } catch (err) {
    if (!(err instanceof RangeError)) {
      throw err;
    }
    throw 'Unable to grow wasm table. Set ALLOW_TABLE_GROWTH.';
  }
  return wasmTable.length - 1;
}

function updateTableMap(offset, count) {
  for (var i = offset; i < offset + count; i++) {
    var item = getWasmTableEntry(i);
    // Ignore null values.
    if (item) {
      functionsInTableMap.set(item, i);
    }
  }
}

/**
 * Add a function to the table.
 * 'sig' parameter is required if the function being added is a JS function.
 * @param {string=} sig
 */
function addFunction(func, sig) {

  // Check if the function is already in the table, to ensure each function
  // gets a unique index. First, create the map if this is the first use.
  if (!functionsInTableMap) {
    functionsInTableMap = new WeakMap();
    updateTableMap(0, wasmTable.length);
  }
  if (functionsInTableMap.has(func)) {
    return functionsInTableMap.get(func);
  }

  // It's not in the table, add it now.

  var ret = getEmptyTableSlot();

  // Set the new value.
  try {
    // Attempting to call this with JS function will cause of table.set() to fail
    setWasmTableEntry(ret, func);
  } catch (err) {
    if (!(err instanceof TypeError)) {
      throw err;
    }
    var wrapped = convertJsFunctionToWasm(func, sig);
    setWasmTableEntry(ret, wrapped);
  }

  functionsInTableMap.set(func, ret);

  return ret;
}

function removeFunction(index) {
  functionsInTableMap.delete(getWasmTableEntry(index));
  freeTableIndexes.push(index);
}

// end include: runtime_functions.js
// include: runtime_debug.js


// end include: runtime_debug.js
var tempRet0 = 0;
var setTempRet0 = (value) => { tempRet0 = value; };
var getTempRet0 = () => tempRet0;



// === Preamble library stuff ===

// Documentation for the public APIs defined in this file must be updated in:
//    site/source/docs/api_reference/preamble.js.rst
// A prebuilt local version of the documentation is available at:
//    site/build/text/docs/api_reference/preamble.js.txt
// You can also build docs locally as HTML or other formats in site/
// An online HTML version (which may be of a different version of Emscripten)
//    is up at http://kripken.github.io/emscripten-site/docs/api_reference/preamble.js.html

var wasmBinary;
if (Module['wasmBinary']) wasmBinary = Module['wasmBinary'];
var noExitRuntime = Module['noExitRuntime'] || true;

if (typeof WebAssembly != 'object') {
  abort('no native wasm support detected');
}

// Wasm globals

var wasmMemory;

//========================================
// Runtime essentials
//========================================

// whether we are quitting the application. no code should run after this.
// set in exit() and abort()
var ABORT = false;

// set by exit() and abort().  Passed to 'onExit' handler.
// NOTE: This is also used as the process return code code in shell environments
// but only when noExitRuntime is false.
var EXITSTATUS;

/** @type {function(*, string=)} */
function assert(condition, text) {
  if (!condition) {
    // This build was created without ASSERTIONS defined.  `assert()` should not
    // ever be called in this configuration but in case there are callers in
    // the wild leave this simple abort() implemenation here for now.
    abort(text);
  }
}

// Returns the C function with a specified identifier (for C++, you need to do manual name mangling)
function getCFunc(ident) {
  var func = Module['_' + ident]; // closure exported function
  return func;
}

// C calling interface.
/** @param {string|null=} returnType
    @param {Array=} argTypes
    @param {Arguments|Array=} args
    @param {Object=} opts */
function ccall(ident, returnType, argTypes, args, opts) {
  // For fast lookup of conversion functions
  var toC = {
    'string': function(str) {
      var ret = 0;
      if (str !== null && str !== undefined && str !== 0) { // null string
        // at most 4 bytes per UTF-8 code point, +1 for the trailing '\0'
        var len = (str.length << 2) + 1;
        ret = stackAlloc(len);
        stringToUTF8(str, ret, len);
      }
      return ret;
    },
    'array': function(arr) {
      var ret = stackAlloc(arr.length);
      writeArrayToMemory(arr, ret);
      return ret;
    }
  };

  function convertReturnValue(ret) {
    if (returnType === 'string') {
      
      return UTF8ToString(ret);
    }
    if (returnType === 'boolean') return Boolean(ret);
    return ret;
  }

  var func = getCFunc(ident);
  var cArgs = [];
  var stack = 0;
  if (args) {
    for (var i = 0; i < args.length; i++) {
      var converter = toC[argTypes[i]];
      if (converter) {
        if (stack === 0) stack = stackSave();
        cArgs[i] = converter(args[i]);
      } else {
        cArgs[i] = args[i];
      }
    }
  }
  // Data for a previous async operation that was in flight before us.
  var previousAsync = Asyncify.currData;
  var ret = func.apply(null, cArgs);
  function onDone(ret) {
    runtimeKeepalivePop();
    if (stack !== 0) stackRestore(stack);
    return convertReturnValue(ret);
  }
  // Keep the runtime alive through all calls. Note that this call might not be
  // async, but for simplicity we push and pop in all calls.
  runtimeKeepalivePush();
  var asyncMode = opts && opts.async;
  if (Asyncify.currData != previousAsync) {
    // This is a new async operation. The wasm is paused and has unwound its stack.
    // We need to return a Promise that resolves the return value
    // once the stack is rewound and execution finishes.
    return Asyncify.whenDone().then(onDone);
  }

  ret = onDone(ret);
  // If this is an async ccall, ensure we return a promise
  if (asyncMode) return Promise.resolve(ret);
  return ret;
}

/** @param {string=} returnType
    @param {Array=} argTypes
    @param {Object=} opts */
function cwrap(ident, returnType, argTypes, opts) {
  argTypes = argTypes || [];
  // When the function takes numbers and returns a number, we can just return
  // the original function
  var numericArgs = argTypes.every(function(type){ return type === 'number'});
  var numericRet = returnType !== 'string';
  if (numericRet && numericArgs && !opts) {
    return getCFunc(ident);
  }
  return function() {
    return ccall(ident, returnType, argTypes, arguments, opts);
  }
}

// include: runtime_legacy.js


var ALLOC_NORMAL = 0; // Tries to use _malloc()
var ALLOC_STACK = 1; // Lives for the duration of the current function call

/**
 * allocate(): This function is no longer used by emscripten but is kept around to avoid
 *             breaking external users.
 *             You should normally not use allocate(), and instead allocate
 *             memory using _malloc()/stackAlloc(), initialize it with
 *             setValue(), and so forth.
 * @param {(Uint8Array|Array<number>)} slab: An array of data.
 * @param {number=} allocator : How to allocate memory, see ALLOC_*
 */
function allocate(slab, allocator) {
  var ret;

  if (allocator == ALLOC_STACK) {
    ret = stackAlloc(slab.length);
  } else {
    ret = _malloc(slab.length);
  }

  if (!slab.subarray && !slab.slice) {
    slab = new Uint8Array(slab);
  }
  HEAPU8.set(slab, ret);
  return ret;
}

// end include: runtime_legacy.js
// include: runtime_strings.js


// runtime_strings.js: Strings related runtime functions that are part of both MINIMAL_RUNTIME and regular runtime.

var UTF8Decoder = typeof TextDecoder != 'undefined' ? new TextDecoder('utf8') : undefined;

// Given a pointer 'ptr' to a null-terminated UTF8-encoded string in the given array that contains uint8 values, returns
// a copy of that string as a Javascript String object.
/**
 * heapOrArray is either a regular array, or a JavaScript typed array view.
 * @param {number} idx
 * @param {number=} maxBytesToRead
 * @return {string}
 */
function UTF8ArrayToString(heapOrArray, idx, maxBytesToRead) {
  var endIdx = idx + maxBytesToRead;
  var endPtr = idx;
  // TextDecoder needs to know the byte length in advance, it doesn't stop on null terminator by itself.
  // Also, use the length info to avoid running tiny strings through TextDecoder, since .subarray() allocates garbage.
  // (As a tiny code save trick, compare endPtr against endIdx using a negation, so that undefined means Infinity)
  while (heapOrArray[endPtr] && !(endPtr >= endIdx)) ++endPtr;

  if (endPtr - idx > 16 && heapOrArray.buffer && UTF8Decoder) {
    return UTF8Decoder.decode(heapOrArray.subarray(idx, endPtr));
  } else {
    var str = '';
    // If building with TextDecoder, we have already computed the string length above, so test loop end condition against that
    while (idx < endPtr) {
      // For UTF8 byte structure, see:
      // http://en.wikipedia.org/wiki/UTF-8#Description
      // https://www.ietf.org/rfc/rfc2279.txt
      // https://tools.ietf.org/html/rfc3629
      var u0 = heapOrArray[idx++];
      if (!(u0 & 0x80)) { str += String.fromCharCode(u0); continue; }
      var u1 = heapOrArray[idx++] & 63;
      if ((u0 & 0xE0) == 0xC0) { str += String.fromCharCode(((u0 & 31) << 6) | u1); continue; }
      var u2 = heapOrArray[idx++] & 63;
      if ((u0 & 0xF0) == 0xE0) {
        u0 = ((u0 & 15) << 12) | (u1 << 6) | u2;
      } else {
        u0 = ((u0 & 7) << 18) | (u1 << 12) | (u2 << 6) | (heapOrArray[idx++] & 63);
      }

      if (u0 < 0x10000) {
        str += String.fromCharCode(u0);
      } else {
        var ch = u0 - 0x10000;
        str += String.fromCharCode(0xD800 | (ch >> 10), 0xDC00 | (ch & 0x3FF));
      }
    }
  }
  return str;
}

// Given a pointer 'ptr' to a null-terminated UTF8-encoded string in the emscripten HEAP, returns a
// copy of that string as a Javascript String object.
// maxBytesToRead: an optional length that specifies the maximum number of bytes to read. You can omit
//                 this parameter to scan the string until the first \0 byte. If maxBytesToRead is
//                 passed, and the string at [ptr, ptr+maxBytesToReadr[ contains a null byte in the
//                 middle, then the string will cut short at that byte index (i.e. maxBytesToRead will
//                 not produce a string of exact length [ptr, ptr+maxBytesToRead[)
//                 N.B. mixing frequent uses of UTF8ToString() with and without maxBytesToRead may
//                 throw JS JIT optimizations off, so it is worth to consider consistently using one
//                 style or the other.
/**
 * @param {number} ptr
 * @param {number=} maxBytesToRead
 * @return {string}
 */
function UTF8ToString(ptr, maxBytesToRead) {
  return ptr ? UTF8ArrayToString(HEAPU8, ptr, maxBytesToRead) : '';
}

// Copies the given Javascript String object 'str' to the given byte array at address 'outIdx',
// encoded in UTF8 form and null-terminated. The copy will require at most str.length*4+1 bytes of space in the HEAP.
// Use the function lengthBytesUTF8 to compute the exact number of bytes (excluding null terminator) that this function will write.
// Parameters:
//   str: the Javascript string to copy.
//   heap: the array to copy to. Each index in this array is assumed to be one 8-byte element.
//   outIdx: The starting offset in the array to begin the copying.
//   maxBytesToWrite: The maximum number of bytes this function can write to the array.
//                    This count should include the null terminator,
//                    i.e. if maxBytesToWrite=1, only the null terminator will be written and nothing else.
//                    maxBytesToWrite=0 does not write any bytes to the output, not even the null terminator.
// Returns the number of bytes written, EXCLUDING the null terminator.

function stringToUTF8Array(str, heap, outIdx, maxBytesToWrite) {
  if (!(maxBytesToWrite > 0)) // Parameter maxBytesToWrite is not optional. Negative values, 0, null, undefined and false each don't write out any bytes.
    return 0;

  var startIdx = outIdx;
  var endIdx = outIdx + maxBytesToWrite - 1; // -1 for string null terminator.
  for (var i = 0; i < str.length; ++i) {
    // Gotcha: charCodeAt returns a 16-bit word that is a UTF-16 encoded code unit, not a Unicode code point of the character! So decode UTF16->UTF32->UTF8.
    // See http://unicode.org/faq/utf_bom.html#utf16-3
    // For UTF8 byte structure, see http://en.wikipedia.org/wiki/UTF-8#Description and https://www.ietf.org/rfc/rfc2279.txt and https://tools.ietf.org/html/rfc3629
    var u = str.charCodeAt(i); // possibly a lead surrogate
    if (u >= 0xD800 && u <= 0xDFFF) {
      var u1 = str.charCodeAt(++i);
      u = 0x10000 + ((u & 0x3FF) << 10) | (u1 & 0x3FF);
    }
    if (u <= 0x7F) {
      if (outIdx >= endIdx) break;
      heap[outIdx++] = u;
    } else if (u <= 0x7FF) {
      if (outIdx + 1 >= endIdx) break;
      heap[outIdx++] = 0xC0 | (u >> 6);
      heap[outIdx++] = 0x80 | (u & 63);
    } else if (u <= 0xFFFF) {
      if (outIdx + 2 >= endIdx) break;
      heap[outIdx++] = 0xE0 | (u >> 12);
      heap[outIdx++] = 0x80 | ((u >> 6) & 63);
      heap[outIdx++] = 0x80 | (u & 63);
    } else {
      if (outIdx + 3 >= endIdx) break;
      heap[outIdx++] = 0xF0 | (u >> 18);
      heap[outIdx++] = 0x80 | ((u >> 12) & 63);
      heap[outIdx++] = 0x80 | ((u >> 6) & 63);
      heap[outIdx++] = 0x80 | (u & 63);
    }
  }
  // Null-terminate the pointer to the buffer.
  heap[outIdx] = 0;
  return outIdx - startIdx;
}

// Copies the given Javascript String object 'str' to the emscripten HEAP at address 'outPtr',
// null-terminated and encoded in UTF8 form. The copy will require at most str.length*4+1 bytes of space in the HEAP.
// Use the function lengthBytesUTF8 to compute the exact number of bytes (excluding null terminator) that this function will write.
// Returns the number of bytes written, EXCLUDING the null terminator.

function stringToUTF8(str, outPtr, maxBytesToWrite) {
  return stringToUTF8Array(str, HEAPU8,outPtr, maxBytesToWrite);
}

// Returns the number of bytes the given Javascript string takes if encoded as a UTF8 byte array, EXCLUDING the null terminator byte.
function lengthBytesUTF8(str) {
  var len = 0;
  for (var i = 0; i < str.length; ++i) {
    // Gotcha: charCodeAt returns a 16-bit word that is a UTF-16 encoded code unit, not a Unicode code point of the character! So decode UTF16->UTF32->UTF8.
    // See http://unicode.org/faq/utf_bom.html#utf16-3
    var u = str.charCodeAt(i); // possibly a lead surrogate
    if (u >= 0xD800 && u <= 0xDFFF) u = 0x10000 + ((u & 0x3FF) << 10) | (str.charCodeAt(++i) & 0x3FF);
    if (u <= 0x7F) ++len;
    else if (u <= 0x7FF) len += 2;
    else if (u <= 0xFFFF) len += 3;
    else len += 4;
  }
  return len;
}

// end include: runtime_strings.js
// include: runtime_strings_extra.js


// runtime_strings_extra.js: Strings related runtime functions that are available only in regular runtime.

// Given a pointer 'ptr' to a null-terminated ASCII-encoded string in the emscripten HEAP, returns
// a copy of that string as a Javascript String object.

function AsciiToString(ptr) {
  var str = '';
  while (1) {
    var ch = HEAPU8[((ptr++)>>0)];
    if (!ch) return str;
    str += String.fromCharCode(ch);
  }
}

// Copies the given Javascript String object 'str' to the emscripten HEAP at address 'outPtr',
// null-terminated and encoded in ASCII form. The copy will require at most str.length+1 bytes of space in the HEAP.

function stringToAscii(str, outPtr) {
  return writeAsciiToMemory(str, outPtr, false);
}

// Given a pointer 'ptr' to a null-terminated UTF16LE-encoded string in the emscripten HEAP, returns
// a copy of that string as a Javascript String object.

var UTF16Decoder = typeof TextDecoder != 'undefined' ? new TextDecoder('utf-16le') : undefined;

function UTF16ToString(ptr, maxBytesToRead) {
  var endPtr = ptr;
  // TextDecoder needs to know the byte length in advance, it doesn't stop on null terminator by itself.
  // Also, use the length info to avoid running tiny strings through TextDecoder, since .subarray() allocates garbage.
  var idx = endPtr >> 1;
  var maxIdx = idx + maxBytesToRead / 2;
  // If maxBytesToRead is not passed explicitly, it will be undefined, and this
  // will always evaluate to true. This saves on code size.
  while (!(idx >= maxIdx) && HEAPU16[idx]) ++idx;
  endPtr = idx << 1;

  if (endPtr - ptr > 32 && UTF16Decoder) {
    return UTF16Decoder.decode(HEAPU8.subarray(ptr, endPtr));
  } else {
    var str = '';

    // If maxBytesToRead is not passed explicitly, it will be undefined, and the for-loop's condition
    // will always evaluate to true. The loop is then terminated on the first null char.
    for (var i = 0; !(i >= maxBytesToRead / 2); ++i) {
      var codeUnit = HEAP16[(((ptr)+(i*2))>>1)];
      if (codeUnit == 0) break;
      // fromCharCode constructs a character from a UTF-16 code unit, so we can pass the UTF16 string right through.
      str += String.fromCharCode(codeUnit);
    }

    return str;
  }
}

// Copies the given Javascript String object 'str' to the emscripten HEAP at address 'outPtr',
// null-terminated and encoded in UTF16 form. The copy will require at most str.length*4+2 bytes of space in the HEAP.
// Use the function lengthBytesUTF16() to compute the exact number of bytes (excluding null terminator) that this function will write.
// Parameters:
//   str: the Javascript string to copy.
//   outPtr: Byte address in Emscripten HEAP where to write the string to.
//   maxBytesToWrite: The maximum number of bytes this function can write to the array. This count should include the null
//                    terminator, i.e. if maxBytesToWrite=2, only the null terminator will be written and nothing else.
//                    maxBytesToWrite<2 does not write any bytes to the output, not even the null terminator.
// Returns the number of bytes written, EXCLUDING the null terminator.

function stringToUTF16(str, outPtr, maxBytesToWrite) {
  // Backwards compatibility: if max bytes is not specified, assume unsafe unbounded write is allowed.
  if (maxBytesToWrite === undefined) {
    maxBytesToWrite = 0x7FFFFFFF;
  }
  if (maxBytesToWrite < 2) return 0;
  maxBytesToWrite -= 2; // Null terminator.
  var startPtr = outPtr;
  var numCharsToWrite = (maxBytesToWrite < str.length*2) ? (maxBytesToWrite / 2) : str.length;
  for (var i = 0; i < numCharsToWrite; ++i) {
    // charCodeAt returns a UTF-16 encoded code unit, so it can be directly written to the HEAP.
    var codeUnit = str.charCodeAt(i); // possibly a lead surrogate
    HEAP16[((outPtr)>>1)] = codeUnit;
    outPtr += 2;
  }
  // Null-terminate the pointer to the HEAP.
  HEAP16[((outPtr)>>1)] = 0;
  return outPtr - startPtr;
}

// Returns the number of bytes the given Javascript string takes if encoded as a UTF16 byte array, EXCLUDING the null terminator byte.

function lengthBytesUTF16(str) {
  return str.length*2;
}

function UTF32ToString(ptr, maxBytesToRead) {
  var i = 0;

  var str = '';
  // If maxBytesToRead is not passed explicitly, it will be undefined, and this
  // will always evaluate to true. This saves on code size.
  while (!(i >= maxBytesToRead / 4)) {
    var utf32 = HEAP32[(((ptr)+(i*4))>>2)];
    if (utf32 == 0) break;
    ++i;
    // Gotcha: fromCharCode constructs a character from a UTF-16 encoded code (pair), not from a Unicode code point! So encode the code point to UTF-16 for constructing.
    // See http://unicode.org/faq/utf_bom.html#utf16-3
    if (utf32 >= 0x10000) {
      var ch = utf32 - 0x10000;
      str += String.fromCharCode(0xD800 | (ch >> 10), 0xDC00 | (ch & 0x3FF));
    } else {
      str += String.fromCharCode(utf32);
    }
  }
  return str;
}

// Copies the given Javascript String object 'str' to the emscripten HEAP at address 'outPtr',
// null-terminated and encoded in UTF32 form. The copy will require at most str.length*4+4 bytes of space in the HEAP.
// Use the function lengthBytesUTF32() to compute the exact number of bytes (excluding null terminator) that this function will write.
// Parameters:
//   str: the Javascript string to copy.
//   outPtr: Byte address in Emscripten HEAP where to write the string to.
//   maxBytesToWrite: The maximum number of bytes this function can write to the array. This count should include the null
//                    terminator, i.e. if maxBytesToWrite=4, only the null terminator will be written and nothing else.
//                    maxBytesToWrite<4 does not write any bytes to the output, not even the null terminator.
// Returns the number of bytes written, EXCLUDING the null terminator.

function stringToUTF32(str, outPtr, maxBytesToWrite) {
  // Backwards compatibility: if max bytes is not specified, assume unsafe unbounded write is allowed.
  if (maxBytesToWrite === undefined) {
    maxBytesToWrite = 0x7FFFFFFF;
  }
  if (maxBytesToWrite < 4) return 0;
  var startPtr = outPtr;
  var endPtr = startPtr + maxBytesToWrite - 4;
  for (var i = 0; i < str.length; ++i) {
    // Gotcha: charCodeAt returns a 16-bit word that is a UTF-16 encoded code unit, not a Unicode code point of the character! We must decode the string to UTF-32 to the heap.
    // See http://unicode.org/faq/utf_bom.html#utf16-3
    var codeUnit = str.charCodeAt(i); // possibly a lead surrogate
    if (codeUnit >= 0xD800 && codeUnit <= 0xDFFF) {
      var trailSurrogate = str.charCodeAt(++i);
      codeUnit = 0x10000 + ((codeUnit & 0x3FF) << 10) | (trailSurrogate & 0x3FF);
    }
    HEAP32[((outPtr)>>2)] = codeUnit;
    outPtr += 4;
    if (outPtr + 4 > endPtr) break;
  }
  // Null-terminate the pointer to the HEAP.
  HEAP32[((outPtr)>>2)] = 0;
  return outPtr - startPtr;
}

// Returns the number of bytes the given Javascript string takes if encoded as a UTF16 byte array, EXCLUDING the null terminator byte.

function lengthBytesUTF32(str) {
  var len = 0;
  for (var i = 0; i < str.length; ++i) {
    // Gotcha: charCodeAt returns a 16-bit word that is a UTF-16 encoded code unit, not a Unicode code point of the character! We must decode the string to UTF-32 to the heap.
    // See http://unicode.org/faq/utf_bom.html#utf16-3
    var codeUnit = str.charCodeAt(i);
    if (codeUnit >= 0xD800 && codeUnit <= 0xDFFF) ++i; // possibly a lead surrogate, so skip over the tail surrogate.
    len += 4;
  }

  return len;
}

// Allocate heap space for a JS string, and write it there.
// It is the responsibility of the caller to free() that memory.
function allocateUTF8(str) {
  var size = lengthBytesUTF8(str) + 1;
  var ret = _malloc(size);
  if (ret) stringToUTF8Array(str, HEAP8, ret, size);
  return ret;
}

// Allocate stack space for a JS string, and write it there.
function allocateUTF8OnStack(str) {
  var size = lengthBytesUTF8(str) + 1;
  var ret = stackAlloc(size);
  stringToUTF8Array(str, HEAP8, ret, size);
  return ret;
}

// Deprecated: This function should not be called because it is unsafe and does not provide
// a maximum length limit of how many bytes it is allowed to write. Prefer calling the
// function stringToUTF8Array() instead, which takes in a maximum length that can be used
// to be secure from out of bounds writes.
/** @deprecated
    @param {boolean=} dontAddNull */
function writeStringToMemory(string, buffer, dontAddNull) {
  warnOnce('writeStringToMemory is deprecated and should not be called! Use stringToUTF8() instead!');

  var /** @type {number} */ lastChar, /** @type {number} */ end;
  if (dontAddNull) {
    // stringToUTF8Array always appends null. If we don't want to do that, remember the
    // character that existed at the location where the null will be placed, and restore
    // that after the write (below).
    end = buffer + lengthBytesUTF8(string);
    lastChar = HEAP8[end];
  }
  stringToUTF8(string, buffer, Infinity);
  if (dontAddNull) HEAP8[end] = lastChar; // Restore the value under the null character.
}

function writeArrayToMemory(array, buffer) {
  HEAP8.set(array, buffer);
}

/** @param {boolean=} dontAddNull */
function writeAsciiToMemory(str, buffer, dontAddNull) {
  for (var i = 0; i < str.length; ++i) {
    HEAP8[((buffer++)>>0)] = str.charCodeAt(i);
  }
  // Null-terminate the pointer to the HEAP.
  if (!dontAddNull) HEAP8[((buffer)>>0)] = 0;
}

// end include: runtime_strings_extra.js
// Memory management

var HEAP,
/** @type {!ArrayBuffer} */
  buffer,
/** @type {!Int8Array} */
  HEAP8,
/** @type {!Uint8Array} */
  HEAPU8,
/** @type {!Int16Array} */
  HEAP16,
/** @type {!Uint16Array} */
  HEAPU16,
/** @type {!Int32Array} */
  HEAP32,
/** @type {!Uint32Array} */
  HEAPU32,
/** @type {!Float32Array} */
  HEAPF32,
/** @type {!Float64Array} */
  HEAPF64;

function updateGlobalBufferAndViews(buf) {
  buffer = buf;
  Module['HEAP8'] = HEAP8 = new Int8Array(buf);
  Module['HEAP16'] = HEAP16 = new Int16Array(buf);
  Module['HEAP32'] = HEAP32 = new Int32Array(buf);
  Module['HEAPU8'] = HEAPU8 = new Uint8Array(buf);
  Module['HEAPU16'] = HEAPU16 = new Uint16Array(buf);
  Module['HEAPU32'] = HEAPU32 = new Uint32Array(buf);
  Module['HEAPF32'] = HEAPF32 = new Float32Array(buf);
  Module['HEAPF64'] = HEAPF64 = new Float64Array(buf);
}

var TOTAL_STACK = 5242880;

var INITIAL_MEMORY = Module['INITIAL_MEMORY'] || 134217728;

// include: runtime_init_table.js
// In regular non-RELOCATABLE mode the table is exported
// from the wasm module and this will be assigned once
// the exports are available.
var wasmTable;

// end include: runtime_init_table.js
// include: runtime_stack_check.js


// end include: runtime_stack_check.js
// include: runtime_assertions.js


// end include: runtime_assertions.js
var __ATPRERUN__  = []; // functions called before the runtime is initialized
var __ATINIT__    = []; // functions called during startup
var __ATEXIT__    = []; // functions called during shutdown
var __ATPOSTRUN__ = []; // functions called after the main() is called

var runtimeInitialized = false;

function keepRuntimeAlive() {
  return noExitRuntime;
}

function preRun() {

  if (Module['preRun']) {
    if (typeof Module['preRun'] == 'function') Module['preRun'] = [Module['preRun']];
    while (Module['preRun'].length) {
      addOnPreRun(Module['preRun'].shift());
    }
  }

  callRuntimeCallbacks(__ATPRERUN__);
}

function initRuntime() {
  runtimeInitialized = true;

  SOCKFS.root = FS.mount(SOCKFS, {}, null);

if (!Module["noFSInit"] && !FS.init.initialized)
  FS.init();
FS.ignorePermissions = false;

TTY.init();
  callRuntimeCallbacks(__ATINIT__);
}

function postRun() {

  if (Module['postRun']) {
    if (typeof Module['postRun'] == 'function') Module['postRun'] = [Module['postRun']];
    while (Module['postRun'].length) {
      addOnPostRun(Module['postRun'].shift());
    }
  }

  callRuntimeCallbacks(__ATPOSTRUN__);
}

function addOnPreRun(cb) {
  __ATPRERUN__.unshift(cb);
}

function addOnInit(cb) {
  __ATINIT__.unshift(cb);
}

function addOnExit(cb) {
}

function addOnPostRun(cb) {
  __ATPOSTRUN__.unshift(cb);
}

// include: runtime_math.js


// https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/Math/imul

// https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/Math/fround

// https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/Math/clz32

// https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/Math/trunc

// end include: runtime_math.js
// A counter of dependencies for calling run(). If we need to
// do asynchronous work before running, increment this and
// decrement it. Incrementing must happen in a place like
// Module.preRun (used by emcc to add file preloading).
// Note that you can add dependencies in preRun, even though
// it happens right before run - run will be postponed until
// the dependencies are met.
var runDependencies = 0;
var runDependencyWatcher = null;
var dependenciesFulfilled = null; // overridden to take different actions when all run dependencies are fulfilled

function getUniqueRunDependency(id) {
  return id;
}

function addRunDependency(id) {
  runDependencies++;

  if (Module['monitorRunDependencies']) {
    Module['monitorRunDependencies'](runDependencies);
  }

}

function removeRunDependency(id) {
  runDependencies--;

  if (Module['monitorRunDependencies']) {
    Module['monitorRunDependencies'](runDependencies);
  }

  if (runDependencies == 0) {
    if (runDependencyWatcher !== null) {
      clearInterval(runDependencyWatcher);
      runDependencyWatcher = null;
    }
    if (dependenciesFulfilled) {
      var callback = dependenciesFulfilled;
      dependenciesFulfilled = null;
      callback(); // can add another dependenciesFulfilled
    }
  }
}

/** @param {string|number=} what */
function abort(what) {
  {
    if (Module['onAbort']) {
      Module['onAbort'](what);
    }
  }

  what = 'Aborted(' + what + ')';
  // TODO(sbc): Should we remove printing and leave it up to whoever
  // catches the exception?
  err(what);

  ABORT = true;
  EXITSTATUS = 1;

  what += '. Build with -sASSERTIONS for more info.';

  // Use a wasm runtime error, because a JS error might be seen as a foreign
  // exception, which means we'd run destructors on it. We need the error to
  // simply make the program stop.
  // FIXME This approach does not work in Wasm EH because it currently does not assume
  // all RuntimeErrors are from traps; it decides whether a RuntimeError is from
  // a trap or not based on a hidden field within the object. So at the moment
  // we don't have a way of throwing a wasm trap from JS. TODO Make a JS API that
  // allows this in the wasm spec.

  // Suppress closure compiler warning here. Closure compiler's builtin extern
  // defintion for WebAssembly.RuntimeError claims it takes no arguments even
  // though it can.
  // TODO(https://github.com/google/closure-compiler/pull/3913): Remove if/when upstream closure gets fixed.
  /** @suppress {checkTypes} */
  var e = new WebAssembly.RuntimeError(what);

  readyPromiseReject(e);
  // Throw the error whether or not MODULARIZE is set because abort is used
  // in code paths apart from instantiation where an exception is expected
  // to be thrown when abort is called.
  throw e;
}

// {{MEM_INITIALIZER}}

// include: memoryprofiler.js


// end include: memoryprofiler.js
// include: URIUtils.js


// Prefix of data URIs emitted by SINGLE_FILE and related options.
var dataURIPrefix = 'data:application/octet-stream;base64,';

// Indicates whether filename is a base64 data URI.
function isDataURI(filename) {
  // Prefix of data URIs emitted by SINGLE_FILE and related options.
  return filename.startsWith(dataURIPrefix);
}

// Indicates whether filename is delivered via file protocol (as opposed to http/https)
function isFileURI(filename) {
  return filename.startsWith('file://');
}

// end include: URIUtils.js
var wasmBinaryFile;
if (Module['locateFile']) {
  wasmBinaryFile = 'dotnet.wasm';
  if (!isDataURI(wasmBinaryFile)) {
    wasmBinaryFile = locateFile(wasmBinaryFile);
  }
} else {
  // Use bundler-friendly `new URL(..., import.meta.url)` pattern; works in browsers too.
  wasmBinaryFile = new URL('dotnet.wasm', import.meta.url).toString();
}

function getBinary(file) {
  try {
    if (file == wasmBinaryFile && wasmBinary) {
      return new Uint8Array(wasmBinary);
    }
    if (readBinary) {
      return readBinary(file);
    } else {
      throw "both async and sync fetching of the wasm failed";
    }
  }
  catch (err) {
    abort(err);
  }
}

function getBinaryPromise() {
  // If we don't have the binary yet, try to to load it asynchronously.
  // Fetch has some additional restrictions over XHR, like it can't be used on a file:// url.
  // See https://github.com/github/fetch/pull/92#issuecomment-140665932
  // Cordova or Electron apps are typically loaded from a file:// url.
  // So use fetch if it is available and the url is not a file, otherwise fall back to XHR.
  if (!wasmBinary && (ENVIRONMENT_IS_WEB || ENVIRONMENT_IS_WORKER)) {
    if (typeof fetch == 'function'
      && !isFileURI(wasmBinaryFile)
    ) {
      return fetch(wasmBinaryFile, { credentials: 'same-origin' }).then(function(response) {
        if (!response['ok']) {
          throw "failed to load wasm binary file at '" + wasmBinaryFile + "'";
        }
        return response['arrayBuffer']();
      }).catch(function () {
          return getBinary(wasmBinaryFile);
      });
    }
    else {
      if (readAsync) {
        // fetch is not available or url is file => try XHR (readAsync uses XHR internally)
        return new Promise(function(resolve, reject) {
          readAsync(wasmBinaryFile, function(response) { resolve(new Uint8Array(/** @type{!ArrayBuffer} */(response))) }, reject)
        });
      }
    }
  }

  // Otherwise, getBinary should be able to get it synchronously
  return Promise.resolve().then(function() { return getBinary(wasmBinaryFile); });
}

// Create the wasm instance.
// Receives the wasm imports, returns the exports.
function createWasm() {
  // prepare imports
  var info = {
    'env': asmLibraryArg,
    'wasi_snapshot_preview1': asmLibraryArg,
  };
  // Load the wasm module and create an instance of using native support in the JS engine.
  // handle a generated wasm instance, receiving its exports and
  // performing other necessary setup
  /** @param {WebAssembly.Module=} module*/
  function receiveInstance(instance, module) {
    var exports = instance.exports;

    exports = Asyncify.instrumentWasmExports(exports);

    Module['asm'] = exports;

    wasmMemory = Module['asm']['memory'];
    updateGlobalBufferAndViews(wasmMemory.buffer);

    wasmTable = Module['asm']['__indirect_function_table'];

    addOnInit(Module['asm']['__wasm_call_ctors']);

    removeRunDependency('wasm-instantiate');

  }
  // we can't run yet (except in a pthread, where we have a custom sync instantiator)
  addRunDependency('wasm-instantiate');

  // Prefer streaming instantiation if available.
  function receiveInstantiationResult(result) {
    // 'result' is a ResultObject object which has both the module and instance.
    // receiveInstance() will swap in the exports (to Module.asm) so they can be called
    // TODO: Due to Closure regression https://github.com/google/closure-compiler/issues/3193, the above line no longer optimizes out down to the following line.
    // When the regression is fixed, can restore the above USE_PTHREADS-enabled path.
    receiveInstance(result['instance']);
  }

  function instantiateArrayBuffer(receiver) {
    return getBinaryPromise().then(function(binary) {
      return WebAssembly.instantiate(binary, info);
    }).then(function (instance) {
      return instance;
    }).then(receiver, function(reason) {
      err('failed to asynchronously prepare wasm: ' + reason);

      abort(reason);
    });
  }

  function instantiateAsync() {
    if (!wasmBinary &&
        typeof WebAssembly.instantiateStreaming == 'function' &&
        !isDataURI(wasmBinaryFile) &&
        // Don't use streaming for file:// delivered objects in a webview, fetch them synchronously.
        !isFileURI(wasmBinaryFile) &&
        typeof fetch == 'function') {
      return fetch(wasmBinaryFile, { credentials: 'same-origin' }).then(function(response) {
        // Suppress closure warning here since the upstream definition for
        // instantiateStreaming only allows Promise<Repsponse> rather than
        // an actual Response.
        // TODO(https://github.com/google/closure-compiler/pull/3913): Remove if/when upstream closure is fixed.
        /** @suppress {checkTypes} */
        var result = WebAssembly.instantiateStreaming(response, info);

        return result.then(
          receiveInstantiationResult,
          function(reason) {
            // We expect the most common failure cause to be a bad MIME type for the binary,
            // in which case falling back to ArrayBuffer instantiation should work.
            err('wasm streaming compile failed: ' + reason);
            err('falling back to ArrayBuffer instantiation');
            return instantiateArrayBuffer(receiveInstantiationResult);
          });
      });
    } else {
      return instantiateArrayBuffer(receiveInstantiationResult);
    }
  }

  // User shell pages can write their own Module.instantiateWasm = function(imports, successCallback) callback
  // to manually instantiate the Wasm module themselves. This allows pages to run the instantiation parallel
  // to any other async startup actions they are performing.
  // Also pthreads and wasm workers initialize the wasm instance through this path.
  if (Module['instantiateWasm']) {
    try {
      var exports = Module['instantiateWasm'](info, receiveInstance);
      exports = Asyncify.instrumentWasmExports(exports);
      return exports;
    } catch(e) {
      err('Module.instantiateWasm callback failed with error: ' + e);
      return false;
    }
  }

  // If instantiation fails, reject the module ready promise.
  instantiateAsync().catch(readyPromiseReject);
  return {}; // no exports yet; we'll fill them in later
}

// Globals used by JS i64 conversions (see makeSetValue)
var tempDouble;
var tempI64;

// === Body ===

var ASM_CONSTS = {
  
};
function GetCanvasHeight() { return canvas.clientHeight; }
function GetCanvasWidth() { return canvas.clientWidth; }





  function callRuntimeCallbacks(callbacks) {
      while (callbacks.length > 0) {
        var callback = callbacks.shift();
        if (typeof callback == 'function') {
          callback(Module); // Pass the module as the first argument.
          continue;
        }
        var func = callback.func;
        if (typeof func == 'number') {
          if (callback.arg === undefined) {
            // Run the wasm function ptr with signature 'v'. If no function
            // with such signature was exported, this call does not need
            // to be emitted (and would confuse Closure)
            (function() {  dynCall_v.call(null, func); })();
          } else {
            // If any function with signature 'vi' was exported, run
            // the callback with that signature.
            (function(a1) {  dynCall_vi.apply(null, [func, a1]); })(callback.arg);
          }
        } else {
          func(callback.arg === undefined ? null : callback.arg);
        }
      }
    }

  function withStackSave(f) {
      var stack = stackSave();
      var ret = f();
      stackRestore(stack);
      return ret;
    }
  function demangle(func) {
      return func;
    }

  function demangleAll(text) {
      var regex =
        /\b_Z[\w\d_]+/g;
      return text.replace(regex,
        function(x) {
          var y = demangle(x);
          return x === y ? x : (y + ' [' + x + ']');
        });
    }

  
    /**
     * @param {number} ptr
     * @param {string} type
     */
  function getValue(ptr, type = 'i8') {
      if (type.endsWith('*')) type = 'i32';
      switch (type) {
        case 'i1': return HEAP8[((ptr)>>0)];
        case 'i8': return HEAP8[((ptr)>>0)];
        case 'i16': return HEAP16[((ptr)>>1)];
        case 'i32': return HEAP32[((ptr)>>2)];
        case 'i64': return HEAP32[((ptr)>>2)];
        case 'float': return HEAPF32[((ptr)>>2)];
        case 'double': return Number(HEAPF64[((ptr)>>3)]);
        default: abort('invalid type for getValue: ' + type);
      }
      return null;
    }

  var wasmTableMirror = [];
  function getWasmTableEntry(funcPtr) {
      var func = wasmTableMirror[funcPtr];
      if (!func) {
        if (funcPtr >= wasmTableMirror.length) wasmTableMirror.length = funcPtr + 1;
        wasmTableMirror[funcPtr] = func = wasmTable.get(funcPtr);
      }
      return func;
    }

  function handleException(e) {
      // Certain exception types we do not treat as errors since they are used for
      // internal control flow.
      // 1. ExitStatus, which is thrown by exit()
      // 2. "unwind", which is thrown by emscripten_unwind_to_js_event_loop() and others
      //    that wish to return to JS event loop.
      if (e instanceof ExitStatus || e == 'unwind') {
        return EXITSTATUS;
      }
      quit_(1, e);
    }

  function jsStackTrace() {
      var error = new Error();
      if (!error.stack) {
        // IE10+ special cases: It does have callstack info, but it is only
        // populated if an Error object is thrown, so try that as a special-case.
        try {
          throw new Error();
        } catch(e) {
          error = e;
        }
        if (!error.stack) {
          return '(no stack trace available)';
        }
      }
      return error.stack.toString();
    }

  
    /**
     * @param {number} ptr
     * @param {number} value
     * @param {string} type
     */
  function setValue(ptr, value, type = 'i8') {
      if (type.endsWith('*')) type = 'i32';
      switch (type) {
        case 'i1': HEAP8[((ptr)>>0)] = value; break;
        case 'i8': HEAP8[((ptr)>>0)] = value; break;
        case 'i16': HEAP16[((ptr)>>1)] = value; break;
        case 'i32': HEAP32[((ptr)>>2)] = value; break;
        case 'i64': (tempI64 = [value>>>0,(tempDouble=value,(+(Math.abs(tempDouble))) >= 1.0 ? (tempDouble > 0.0 ? ((Math.min((+(Math.floor((tempDouble)/4294967296.0))), 4294967295.0))|0)>>>0 : (~~((+(Math.ceil((tempDouble - +(((~~(tempDouble)))>>>0))/4294967296.0)))))>>>0) : 0)],HEAP32[((ptr)>>2)] = tempI64[0],HEAP32[(((ptr)+(4))>>2)] = tempI64[1]); break;
        case 'float': HEAPF32[((ptr)>>2)] = value; break;
        case 'double': HEAPF64[((ptr)>>3)] = value; break;
        default: abort('invalid type for setValue: ' + type);
      }
    }

  function setWasmTableEntry(idx, func) {
      wasmTable.set(idx, func);
      // With ABORT_ON_WASM_EXCEPTIONS wasmTable.get is overriden to return wrapped
      // functions so we need to call it here to retrieve the potential wrapper correctly
      // instead of just storing 'func' directly into wasmTableMirror
      wasmTableMirror[idx] = wasmTable.get(idx);
    }

  function stackTrace() {
      var js = jsStackTrace();
      if (Module['extraStackTrace']) js += '\n' + Module['extraStackTrace']();
      return demangleAll(js);
    }

  function ___assert_fail(condition, filename, line, func) {
      abort('Assertion failed: ' + UTF8ToString(condition) + ', at: ' + [filename ? UTF8ToString(filename) : 'unknown filename', line, func ? UTF8ToString(func) : 'unknown function']);
    }

  function ___cxa_allocate_exception(size) {
      // Thrown object is prepended by exception metadata block
      return _malloc(size + 24) + 24;
    }

  var exceptionCaught =  [];
  
  function exception_addRef(info) {
      info.add_ref();
    }
  
  var uncaughtExceptionCount = 0;
  function ___cxa_begin_catch(ptr) {
      var info = new ExceptionInfo(ptr);
      if (!info.get_caught()) {
        info.set_caught(true);
        uncaughtExceptionCount--;
      }
      info.set_rethrown(false);
      exceptionCaught.push(info);
      exception_addRef(info);
      return info.get_exception_ptr();
    }

  var exceptionLast = 0;
  
  /** @constructor */
  function ExceptionInfo(excPtr) {
      this.excPtr = excPtr;
      this.ptr = excPtr - 24;
  
      this.set_type = function(type) {
        HEAPU32[(((this.ptr)+(4))>>2)] = type;
      };
  
      this.get_type = function() {
        return HEAPU32[(((this.ptr)+(4))>>2)];
      };
  
      this.set_destructor = function(destructor) {
        HEAPU32[(((this.ptr)+(8))>>2)] = destructor;
      };
  
      this.get_destructor = function() {
        return HEAPU32[(((this.ptr)+(8))>>2)];
      };
  
      this.set_refcount = function(refcount) {
        HEAP32[((this.ptr)>>2)] = refcount;
      };
  
      this.set_caught = function (caught) {
        caught = caught ? 1 : 0;
        HEAP8[(((this.ptr)+(12))>>0)] = caught;
      };
  
      this.get_caught = function () {
        return HEAP8[(((this.ptr)+(12))>>0)] != 0;
      };
  
      this.set_rethrown = function (rethrown) {
        rethrown = rethrown ? 1 : 0;
        HEAP8[(((this.ptr)+(13))>>0)] = rethrown;
      };
  
      this.get_rethrown = function () {
        return HEAP8[(((this.ptr)+(13))>>0)] != 0;
      };
  
      // Initialize native structure fields. Should be called once after allocated.
      this.init = function(type, destructor) {
        this.set_adjusted_ptr(0);
        this.set_type(type);
        this.set_destructor(destructor);
        this.set_refcount(0);
        this.set_caught(false);
        this.set_rethrown(false);
      }
  
      this.add_ref = function() {
        var value = HEAP32[((this.ptr)>>2)];
        HEAP32[((this.ptr)>>2)] = value + 1;
      };
  
      // Returns true if last reference released.
      this.release_ref = function() {
        var prev = HEAP32[((this.ptr)>>2)];
        HEAP32[((this.ptr)>>2)] = prev - 1;
        return prev === 1;
      };
  
      this.set_adjusted_ptr = function(adjustedPtr) {
        HEAPU32[(((this.ptr)+(16))>>2)] = adjustedPtr;
      };
  
      this.get_adjusted_ptr = function() {
        return HEAPU32[(((this.ptr)+(16))>>2)];
      };
  
      // Get pointer which is expected to be received by catch clause in C++ code. It may be adjusted
      // when the pointer is casted to some of the exception object base classes (e.g. when virtual
      // inheritance is used). When a pointer is thrown this method should return the thrown pointer
      // itself.
      this.get_exception_ptr = function() {
        // Work around a fastcomp bug, this code is still included for some reason in a build without
        // exceptions support.
        var isPointer = ___cxa_is_pointer_type(this.get_type());
        if (isPointer) {
          return HEAPU32[((this.excPtr)>>2)];
        }
        var adjusted = this.get_adjusted_ptr();
        if (adjusted !== 0) return adjusted;
        return this.excPtr;
      };
    }
  function ___cxa_free_exception(ptr) {
        return _free(new ExceptionInfo(ptr).ptr);
    }
  function exception_decRef(info) {
      // A rethrown exception can reach refcount 0; it must not be discarded
      // Its next handler will clear the rethrown flag and addRef it, prior to
      // final decRef and destruction here
      if (info.release_ref() && !info.get_rethrown()) {
        var destructor = info.get_destructor();
        if (destructor) {
          // In Wasm, destructors return 'this' as in ARM
          (function(a1) { return dynCall_ii.apply(null, [destructor, a1]); })(info.excPtr);
        }
        ___cxa_free_exception(info.excPtr);
      }
    }
  function ___cxa_end_catch() {
      // Clear state flag.
      _setThrew(0);
      // Call destructor if one is registered then clear it.
      var info = exceptionCaught.pop();
  
      exception_decRef(info);
      exceptionLast = 0; // XXX in decRef?
    }

  function ___resumeException(ptr) {
      if (!exceptionLast) { exceptionLast = ptr; }
      throw ptr;
    }
  function ___cxa_find_matching_catch_3() {
      var thrown = exceptionLast;
      if (!thrown) {
        // just pass through the null ptr
        setTempRet0(0);
        return 0;
      }
      var info = new ExceptionInfo(thrown);
      info.set_adjusted_ptr(thrown);
      var thrownType = info.get_type();
      if (!thrownType) {
        // just pass through the thrown ptr
        setTempRet0(0);
        return thrown;
      }
      var typeArray = Array.prototype.slice.call(arguments);
  
      // can_catch receives a **, add indirection
      // The different catch blocks are denoted by different types.
      // Due to inheritance, those types may not precisely match the
      // type of the thrown object. Find one which matches, and
      // return the type of the catch block which should be called.
      for (var i = 0; i < typeArray.length; i++) {
        var caughtType = typeArray[i];
        if (caughtType === 0 || caughtType === thrownType) {
          // Catch all clause matched or exactly the same type is caught
          break;
        }
        var adjusted_ptr_addr = info.ptr + 16;
        if (___cxa_can_catch(caughtType, thrownType, adjusted_ptr_addr)) {
          setTempRet0(caughtType);
          return thrown;
        }
      }
      setTempRet0(thrownType);
      return thrown;
    }

  function ___cxa_throw(ptr, type, destructor) {
      var info = new ExceptionInfo(ptr);
      // Initialize ExceptionInfo content after it was allocated in __cxa_allocate_exception.
      info.init(type, destructor);
      exceptionLast = ptr;
      uncaughtExceptionCount++;
      throw ptr;
    }


  function getRandomDevice() {
      if (typeof crypto == 'object' && typeof crypto['getRandomValues'] == 'function') {
        // for modern web browsers
        var randomBuffer = new Uint8Array(1);
        return function() { crypto.getRandomValues(randomBuffer); return randomBuffer[0]; };
      } else
      if (ENVIRONMENT_IS_NODE) {
        // for nodejs with or without crypto support included
        try {
          var crypto_module = require('crypto');
          // nodejs has crypto support
          return function() { return crypto_module['randomBytes'](1)[0]; };
        } catch (e) {
          // nodejs doesn't have crypto support
        }
      }
      // we couldn't find a proper implementation, as Math.random() is not suitable for /dev/random, see emscripten-core/emscripten/pull/7096
      return function() { abort("randomDevice"); };
    }
  
  var PATH = {isAbs:(path) => path.charAt(0) === '/',splitPath:(filename) => {
        var splitPathRe = /^(\/?|)([\s\S]*?)((?:\.{1,2}|[^\/]+?|)(\.[^.\/]*|))(?:[\/]*)$/;
        return splitPathRe.exec(filename).slice(1);
      },normalizeArray:(parts, allowAboveRoot) => {
        // if the path tries to go above the root, `up` ends up > 0
        var up = 0;
        for (var i = parts.length - 1; i >= 0; i--) {
          var last = parts[i];
          if (last === '.') {
            parts.splice(i, 1);
          } else if (last === '..') {
            parts.splice(i, 1);
            up++;
          } else if (up) {
            parts.splice(i, 1);
            up--;
          }
        }
        // if the path is allowed to go above the root, restore leading ..s
        if (allowAboveRoot) {
          for (; up; up--) {
            parts.unshift('..');
          }
        }
        return parts;
      },normalize:(path) => {
        var isAbsolute = PATH.isAbs(path),
            trailingSlash = path.substr(-1) === '/';
        // Normalize the path
        path = PATH.normalizeArray(path.split('/').filter((p) => !!p), !isAbsolute).join('/');
        if (!path && !isAbsolute) {
          path = '.';
        }
        if (path && trailingSlash) {
          path += '/';
        }
        return (isAbsolute ? '/' : '') + path;
      },dirname:(path) => {
        var result = PATH.splitPath(path),
            root = result[0],
            dir = result[1];
        if (!root && !dir) {
          // No dirname whatsoever
          return '.';
        }
        if (dir) {
          // It has a dirname, strip trailing slash
          dir = dir.substr(0, dir.length - 1);
        }
        return root + dir;
      },basename:(path) => {
        // EMSCRIPTEN return '/'' for '/', not an empty string
        if (path === '/') return '/';
        path = PATH.normalize(path);
        path = path.replace(/\/$/, "");
        var lastSlash = path.lastIndexOf('/');
        if (lastSlash === -1) return path;
        return path.substr(lastSlash+1);
      },join:function() {
        var paths = Array.prototype.slice.call(arguments, 0);
        return PATH.normalize(paths.join('/'));
      },join2:(l, r) => {
        return PATH.normalize(l + '/' + r);
      }};
  
  var PATH_FS = {resolve:function() {
        var resolvedPath = '',
          resolvedAbsolute = false;
        for (var i = arguments.length - 1; i >= -1 && !resolvedAbsolute; i--) {
          var path = (i >= 0) ? arguments[i] : FS.cwd();
          // Skip empty and invalid entries
          if (typeof path != 'string') {
            throw new TypeError('Arguments to path.resolve must be strings');
          } else if (!path) {
            return ''; // an invalid portion invalidates the whole thing
          }
          resolvedPath = path + '/' + resolvedPath;
          resolvedAbsolute = PATH.isAbs(path);
        }
        // At this point the path should be resolved to a full absolute path, but
        // handle relative paths to be safe (might happen when process.cwd() fails)
        resolvedPath = PATH.normalizeArray(resolvedPath.split('/').filter((p) => !!p), !resolvedAbsolute).join('/');
        return ((resolvedAbsolute ? '/' : '') + resolvedPath) || '.';
      },relative:(from, to) => {
        from = PATH_FS.resolve(from).substr(1);
        to = PATH_FS.resolve(to).substr(1);
        function trim(arr) {
          var start = 0;
          for (; start < arr.length; start++) {
            if (arr[start] !== '') break;
          }
          var end = arr.length - 1;
          for (; end >= 0; end--) {
            if (arr[end] !== '') break;
          }
          if (start > end) return [];
          return arr.slice(start, end - start + 1);
        }
        var fromParts = trim(from.split('/'));
        var toParts = trim(to.split('/'));
        var length = Math.min(fromParts.length, toParts.length);
        var samePartsLength = length;
        for (var i = 0; i < length; i++) {
          if (fromParts[i] !== toParts[i]) {
            samePartsLength = i;
            break;
          }
        }
        var outputParts = [];
        for (var i = samePartsLength; i < fromParts.length; i++) {
          outputParts.push('..');
        }
        outputParts = outputParts.concat(toParts.slice(samePartsLength));
        return outputParts.join('/');
      }};
  
  var TTY = {ttys:[],init:function () {
        // https://github.com/emscripten-core/emscripten/pull/1555
        // if (ENVIRONMENT_IS_NODE) {
        //   // currently, FS.init does not distinguish if process.stdin is a file or TTY
        //   // device, it always assumes it's a TTY device. because of this, we're forcing
        //   // process.stdin to UTF8 encoding to at least make stdin reading compatible
        //   // with text files until FS.init can be refactored.
        //   process['stdin']['setEncoding']('utf8');
        // }
      },shutdown:function() {
        // https://github.com/emscripten-core/emscripten/pull/1555
        // if (ENVIRONMENT_IS_NODE) {
        //   // inolen: any idea as to why node -e 'process.stdin.read()' wouldn't exit immediately (with process.stdin being a tty)?
        //   // isaacs: because now it's reading from the stream, you've expressed interest in it, so that read() kicks off a _read() which creates a ReadReq operation
        //   // inolen: I thought read() in that case was a synchronous operation that just grabbed some amount of buffered data if it exists?
        //   // isaacs: it is. but it also triggers a _read() call, which calls readStart() on the handle
        //   // isaacs: do process.stdin.pause() and i'd think it'd probably close the pending call
        //   process['stdin']['pause']();
        // }
      },register:function(dev, ops) {
        TTY.ttys[dev] = { input: [], output: [], ops: ops };
        FS.registerDevice(dev, TTY.stream_ops);
      },stream_ops:{open:function(stream) {
          var tty = TTY.ttys[stream.node.rdev];
          if (!tty) {
            throw new FS.ErrnoError(43);
          }
          stream.tty = tty;
          stream.seekable = false;
        },close:function(stream) {
          // flush any pending line data
          stream.tty.ops.flush(stream.tty);
        },flush:function(stream) {
          stream.tty.ops.flush(stream.tty);
        },read:function(stream, buffer, offset, length, pos /* ignored */) {
          if (!stream.tty || !stream.tty.ops.get_char) {
            throw new FS.ErrnoError(60);
          }
          var bytesRead = 0;
          for (var i = 0; i < length; i++) {
            var result;
            try {
              result = stream.tty.ops.get_char(stream.tty);
            } catch (e) {
              throw new FS.ErrnoError(29);
            }
            if (result === undefined && bytesRead === 0) {
              throw new FS.ErrnoError(6);
            }
            if (result === null || result === undefined) break;
            bytesRead++;
            buffer[offset+i] = result;
          }
          if (bytesRead) {
            stream.node.timestamp = Date.now();
          }
          return bytesRead;
        },write:function(stream, buffer, offset, length, pos) {
          if (!stream.tty || !stream.tty.ops.put_char) {
            throw new FS.ErrnoError(60);
          }
          try {
            for (var i = 0; i < length; i++) {
              stream.tty.ops.put_char(stream.tty, buffer[offset+i]);
            }
          } catch (e) {
            throw new FS.ErrnoError(29);
          }
          if (length) {
            stream.node.timestamp = Date.now();
          }
          return i;
        }},default_tty_ops:{get_char:function(tty) {
          if (!tty.input.length) {
            var result = null;
            if (ENVIRONMENT_IS_NODE) {
              // we will read data by chunks of BUFSIZE
              var BUFSIZE = 256;
              var buf = Buffer.alloc(BUFSIZE);
              var bytesRead = 0;
  
              try {
                bytesRead = fs.readSync(process.stdin.fd, buf, 0, BUFSIZE, -1);
              } catch(e) {
                // Cross-platform differences: on Windows, reading EOF throws an exception, but on other OSes,
                // reading EOF returns 0. Uniformize behavior by treating the EOF exception to return 0.
                if (e.toString().includes('EOF')) bytesRead = 0;
                else throw e;
              }
  
              if (bytesRead > 0) {
                result = buf.slice(0, bytesRead).toString('utf-8');
              } else {
                result = null;
              }
            } else
            if (typeof window != 'undefined' &&
              typeof window.prompt == 'function') {
              // Browser.
              result = window.prompt('Input: ');  // returns null on cancel
              if (result !== null) {
                result += '\n';
              }
            } else if (typeof readline == 'function') {
              // Command line.
              result = readline();
              if (result !== null) {
                result += '\n';
              }
            }
            if (!result) {
              return null;
            }
            tty.input = intArrayFromString(result, true);
          }
          return tty.input.shift();
        },put_char:function(tty, val) {
          if (val === null || val === 10) {
            out(UTF8ArrayToString(tty.output, 0));
            tty.output = [];
          } else {
            if (val != 0) tty.output.push(val); // val == 0 would cut text output off in the middle.
          }
        },flush:function(tty) {
          if (tty.output && tty.output.length > 0) {
            out(UTF8ArrayToString(tty.output, 0));
            tty.output = [];
          }
        }},default_tty1_ops:{put_char:function(tty, val) {
          if (val === null || val === 10) {
            err(UTF8ArrayToString(tty.output, 0));
            tty.output = [];
          } else {
            if (val != 0) tty.output.push(val);
          }
        },flush:function(tty) {
          if (tty.output && tty.output.length > 0) {
            err(UTF8ArrayToString(tty.output, 0));
            tty.output = [];
          }
        }}};
  
  function zeroMemory(address, size) {
      HEAPU8.fill(0, address, address + size);
    }
  
  function alignMemory(size, alignment) {
      return Math.ceil(size / alignment) * alignment;
    }
  function mmapAlloc(size) {
      size = alignMemory(size, 65536);
      var ptr = _emscripten_builtin_memalign(65536, size);
      if (!ptr) return 0;
      zeroMemory(ptr, size);
      return ptr;
    }
  var MEMFS = {ops_table:null,mount:function(mount) {
        return MEMFS.createNode(null, '/', 16384 | 511 /* 0777 */, 0);
      },createNode:function(parent, name, mode, dev) {
        if (FS.isBlkdev(mode) || FS.isFIFO(mode)) {
          // no supported
          throw new FS.ErrnoError(63);
        }
        if (!MEMFS.ops_table) {
          MEMFS.ops_table = {
            dir: {
              node: {
                getattr: MEMFS.node_ops.getattr,
                setattr: MEMFS.node_ops.setattr,
                lookup: MEMFS.node_ops.lookup,
                mknod: MEMFS.node_ops.mknod,
                rename: MEMFS.node_ops.rename,
                unlink: MEMFS.node_ops.unlink,
                rmdir: MEMFS.node_ops.rmdir,
                readdir: MEMFS.node_ops.readdir,
                symlink: MEMFS.node_ops.symlink
              },
              stream: {
                llseek: MEMFS.stream_ops.llseek
              }
            },
            file: {
              node: {
                getattr: MEMFS.node_ops.getattr,
                setattr: MEMFS.node_ops.setattr
              },
              stream: {
                llseek: MEMFS.stream_ops.llseek,
                read: MEMFS.stream_ops.read,
                write: MEMFS.stream_ops.write,
                allocate: MEMFS.stream_ops.allocate,
                mmap: MEMFS.stream_ops.mmap,
                msync: MEMFS.stream_ops.msync
              }
            },
            link: {
              node: {
                getattr: MEMFS.node_ops.getattr,
                setattr: MEMFS.node_ops.setattr,
                readlink: MEMFS.node_ops.readlink
              },
              stream: {}
            },
            chrdev: {
              node: {
                getattr: MEMFS.node_ops.getattr,
                setattr: MEMFS.node_ops.setattr
              },
              stream: FS.chrdev_stream_ops
            }
          };
        }
        var node = FS.createNode(parent, name, mode, dev);
        if (FS.isDir(node.mode)) {
          node.node_ops = MEMFS.ops_table.dir.node;
          node.stream_ops = MEMFS.ops_table.dir.stream;
          node.contents = {};
        } else if (FS.isFile(node.mode)) {
          node.node_ops = MEMFS.ops_table.file.node;
          node.stream_ops = MEMFS.ops_table.file.stream;
          node.usedBytes = 0; // The actual number of bytes used in the typed array, as opposed to contents.length which gives the whole capacity.
          // When the byte data of the file is populated, this will point to either a typed array, or a normal JS array. Typed arrays are preferred
          // for performance, and used by default. However, typed arrays are not resizable like normal JS arrays are, so there is a small disk size
          // penalty involved for appending file writes that continuously grow a file similar to std::vector capacity vs used -scheme.
          node.contents = null; 
        } else if (FS.isLink(node.mode)) {
          node.node_ops = MEMFS.ops_table.link.node;
          node.stream_ops = MEMFS.ops_table.link.stream;
        } else if (FS.isChrdev(node.mode)) {
          node.node_ops = MEMFS.ops_table.chrdev.node;
          node.stream_ops = MEMFS.ops_table.chrdev.stream;
        }
        node.timestamp = Date.now();
        // add the new node to the parent
        if (parent) {
          parent.contents[name] = node;
          parent.timestamp = node.timestamp;
        }
        return node;
      },getFileDataAsTypedArray:function(node) {
        if (!node.contents) return new Uint8Array(0);
        if (node.contents.subarray) return node.contents.subarray(0, node.usedBytes); // Make sure to not return excess unused bytes.
        return new Uint8Array(node.contents);
      },expandFileStorage:function(node, newCapacity) {
        var prevCapacity = node.contents ? node.contents.length : 0;
        if (prevCapacity >= newCapacity) return; // No need to expand, the storage was already large enough.
        // Don't expand strictly to the given requested limit if it's only a very small increase, but instead geometrically grow capacity.
        // For small filesizes (<1MB), perform size*2 geometric increase, but for large sizes, do a much more conservative size*1.125 increase to
        // avoid overshooting the allocation cap by a very large margin.
        var CAPACITY_DOUBLING_MAX = 1024 * 1024;
        newCapacity = Math.max(newCapacity, (prevCapacity * (prevCapacity < CAPACITY_DOUBLING_MAX ? 2.0 : 1.125)) >>> 0);
        if (prevCapacity != 0) newCapacity = Math.max(newCapacity, 256); // At minimum allocate 256b for each file when expanding.
        var oldContents = node.contents;
        node.contents = new Uint8Array(newCapacity); // Allocate new storage.
        if (node.usedBytes > 0) node.contents.set(oldContents.subarray(0, node.usedBytes), 0); // Copy old data over to the new storage.
      },resizeFileStorage:function(node, newSize) {
        if (node.usedBytes == newSize) return;
        if (newSize == 0) {
          node.contents = null; // Fully decommit when requesting a resize to zero.
          node.usedBytes = 0;
        } else {
          var oldContents = node.contents;
          node.contents = new Uint8Array(newSize); // Allocate new storage.
          if (oldContents) {
            node.contents.set(oldContents.subarray(0, Math.min(newSize, node.usedBytes))); // Copy old data over to the new storage.
          }
          node.usedBytes = newSize;
        }
      },node_ops:{getattr:function(node) {
          var attr = {};
          // device numbers reuse inode numbers.
          attr.dev = FS.isChrdev(node.mode) ? node.id : 1;
          attr.ino = node.id;
          attr.mode = node.mode;
          attr.nlink = 1;
          attr.uid = 0;
          attr.gid = 0;
          attr.rdev = node.rdev;
          if (FS.isDir(node.mode)) {
            attr.size = 4096;
          } else if (FS.isFile(node.mode)) {
            attr.size = node.usedBytes;
          } else if (FS.isLink(node.mode)) {
            attr.size = node.link.length;
          } else {
            attr.size = 0;
          }
          attr.atime = new Date(node.timestamp);
          attr.mtime = new Date(node.timestamp);
          attr.ctime = new Date(node.timestamp);
          // NOTE: In our implementation, st_blocks = Math.ceil(st_size/st_blksize),
          //       but this is not required by the standard.
          attr.blksize = 4096;
          attr.blocks = Math.ceil(attr.size / attr.blksize);
          return attr;
        },setattr:function(node, attr) {
          if (attr.mode !== undefined) {
            node.mode = attr.mode;
          }
          if (attr.timestamp !== undefined) {
            node.timestamp = attr.timestamp;
          }
          if (attr.size !== undefined) {
            MEMFS.resizeFileStorage(node, attr.size);
          }
        },lookup:function(parent, name) {
          throw FS.genericErrors[44];
        },mknod:function(parent, name, mode, dev) {
          return MEMFS.createNode(parent, name, mode, dev);
        },rename:function(old_node, new_dir, new_name) {
          // if we're overwriting a directory at new_name, make sure it's empty.
          if (FS.isDir(old_node.mode)) {
            var new_node;
            try {
              new_node = FS.lookupNode(new_dir, new_name);
            } catch (e) {
            }
            if (new_node) {
              for (var i in new_node.contents) {
                throw new FS.ErrnoError(55);
              }
            }
          }
          // do the internal rewiring
          delete old_node.parent.contents[old_node.name];
          old_node.parent.timestamp = Date.now()
          old_node.name = new_name;
          new_dir.contents[new_name] = old_node;
          new_dir.timestamp = old_node.parent.timestamp;
          old_node.parent = new_dir;
        },unlink:function(parent, name) {
          delete parent.contents[name];
          parent.timestamp = Date.now();
        },rmdir:function(parent, name) {
          var node = FS.lookupNode(parent, name);
          for (var i in node.contents) {
            throw new FS.ErrnoError(55);
          }
          delete parent.contents[name];
          parent.timestamp = Date.now();
        },readdir:function(node) {
          var entries = ['.', '..'];
          for (var key in node.contents) {
            if (!node.contents.hasOwnProperty(key)) {
              continue;
            }
            entries.push(key);
          }
          return entries;
        },symlink:function(parent, newname, oldpath) {
          var node = MEMFS.createNode(parent, newname, 511 /* 0777 */ | 40960, 0);
          node.link = oldpath;
          return node;
        },readlink:function(node) {
          if (!FS.isLink(node.mode)) {
            throw new FS.ErrnoError(28);
          }
          return node.link;
        }},stream_ops:{read:function(stream, buffer, offset, length, position) {
          var contents = stream.node.contents;
          if (position >= stream.node.usedBytes) return 0;
          var size = Math.min(stream.node.usedBytes - position, length);
          if (size > 8 && contents.subarray) { // non-trivial, and typed array
            buffer.set(contents.subarray(position, position + size), offset);
          } else {
            for (var i = 0; i < size; i++) buffer[offset + i] = contents[position + i];
          }
          return size;
        },write:function(stream, buffer, offset, length, position, canOwn) {
          // If the buffer is located in main memory (HEAP), and if
          // memory can grow, we can't hold on to references of the
          // memory buffer, as they may get invalidated. That means we
          // need to do copy its contents.
          if (buffer.buffer === HEAP8.buffer) {
            canOwn = false;
          }
  
          if (!length) return 0;
          var node = stream.node;
          node.timestamp = Date.now();
  
          if (buffer.subarray && (!node.contents || node.contents.subarray)) { // This write is from a typed array to a typed array?
            if (canOwn) {
              node.contents = buffer.subarray(offset, offset + length);
              node.usedBytes = length;
              return length;
            } else if (node.usedBytes === 0 && position === 0) { // If this is a simple first write to an empty file, do a fast set since we don't need to care about old data.
              node.contents = buffer.slice(offset, offset + length);
              node.usedBytes = length;
              return length;
            } else if (position + length <= node.usedBytes) { // Writing to an already allocated and used subrange of the file?
              node.contents.set(buffer.subarray(offset, offset + length), position);
              return length;
            }
          }
  
          // Appending to an existing file and we need to reallocate, or source data did not come as a typed array.
          MEMFS.expandFileStorage(node, position+length);
          if (node.contents.subarray && buffer.subarray) {
            // Use typed array write which is available.
            node.contents.set(buffer.subarray(offset, offset + length), position);
          } else {
            for (var i = 0; i < length; i++) {
             node.contents[position + i] = buffer[offset + i]; // Or fall back to manual write if not.
            }
          }
          node.usedBytes = Math.max(node.usedBytes, position + length);
          return length;
        },llseek:function(stream, offset, whence) {
          var position = offset;
          if (whence === 1) {
            position += stream.position;
          } else if (whence === 2) {
            if (FS.isFile(stream.node.mode)) {
              position += stream.node.usedBytes;
            }
          }
          if (position < 0) {
            throw new FS.ErrnoError(28);
          }
          return position;
        },allocate:function(stream, offset, length) {
          MEMFS.expandFileStorage(stream.node, offset + length);
          stream.node.usedBytes = Math.max(stream.node.usedBytes, offset + length);
        },mmap:function(stream, length, position, prot, flags) {
          if (!FS.isFile(stream.node.mode)) {
            throw new FS.ErrnoError(43);
          }
          var ptr;
          var allocated;
          var contents = stream.node.contents;
          // Only make a new copy when MAP_PRIVATE is specified.
          if (!(flags & 2) && contents.buffer === buffer) {
            // We can't emulate MAP_SHARED when the file is not backed by the buffer
            // we're mapping to (e.g. the HEAP buffer).
            allocated = false;
            ptr = contents.byteOffset;
          } else {
            // Try to avoid unnecessary slices.
            if (position > 0 || position + length < contents.length) {
              if (contents.subarray) {
                contents = contents.subarray(position, position + length);
              } else {
                contents = Array.prototype.slice.call(contents, position, position + length);
              }
            }
            allocated = true;
            ptr = mmapAlloc(length);
            if (!ptr) {
              throw new FS.ErrnoError(48);
            }
            HEAP8.set(contents, ptr);
          }
          return { ptr: ptr, allocated: allocated };
        },msync:function(stream, buffer, offset, length, mmapFlags) {
          if (!FS.isFile(stream.node.mode)) {
            throw new FS.ErrnoError(43);
          }
          if (mmapFlags & 2) {
            // MAP_PRIVATE calls need not to be synced back to underlying fs
            return 0;
          }
  
          var bytesWritten = MEMFS.stream_ops.write(stream, buffer, 0, length, offset, false);
          // should we check if bytesWritten and length are the same?
          return 0;
        }}};
  
  /** @param {boolean=} noRunDep */
  function asyncLoad(url, onload, onerror, noRunDep) {
      var dep = !noRunDep ? getUniqueRunDependency('al ' + url) : '';
      readAsync(url, function(arrayBuffer) {
        assert(arrayBuffer, 'Loading data file "' + url + '" failed (no arrayBuffer).');
        onload(new Uint8Array(arrayBuffer));
        if (dep) removeRunDependency(dep);
      }, function(event) {
        if (onerror) {
          onerror();
        } else {
          throw 'Loading data file "' + url + '" failed.';
        }
      });
      if (dep) addRunDependency(dep);
    }
  var FS = {root:null,mounts:[],devices:{},streams:[],nextInode:1,nameTable:null,currentPath:"/",initialized:false,ignorePermissions:true,ErrnoError:null,genericErrors:{},filesystems:null,syncFSRequests:0,lookupPath:(path, opts = {}) => {
        path = PATH_FS.resolve(FS.cwd(), path);
  
        if (!path) return { path: '', node: null };
  
        var defaults = {
          follow_mount: true,
          recurse_count: 0
        };
        opts = Object.assign(defaults, opts)
  
        if (opts.recurse_count > 8) {  // max recursive lookup of 8
          throw new FS.ErrnoError(32);
        }
  
        // split the path
        var parts = PATH.normalizeArray(path.split('/').filter((p) => !!p), false);
  
        // start at the root
        var current = FS.root;
        var current_path = '/';
  
        for (var i = 0; i < parts.length; i++) {
          var islast = (i === parts.length-1);
          if (islast && opts.parent) {
            // stop resolving
            break;
          }
  
          current = FS.lookupNode(current, parts[i]);
          current_path = PATH.join2(current_path, parts[i]);
  
          // jump to the mount's root node if this is a mountpoint
          if (FS.isMountpoint(current)) {
            if (!islast || (islast && opts.follow_mount)) {
              current = current.mounted.root;
            }
          }
  
          // by default, lookupPath will not follow a symlink if it is the final path component.
          // setting opts.follow = true will override this behavior.
          if (!islast || opts.follow) {
            var count = 0;
            while (FS.isLink(current.mode)) {
              var link = FS.readlink(current_path);
              current_path = PATH_FS.resolve(PATH.dirname(current_path), link);
  
              var lookup = FS.lookupPath(current_path, { recurse_count: opts.recurse_count + 1 });
              current = lookup.node;
  
              if (count++ > 40) {  // limit max consecutive symlinks to 40 (SYMLOOP_MAX).
                throw new FS.ErrnoError(32);
              }
            }
          }
        }
  
        return { path: current_path, node: current };
      },getPath:(node) => {
        var path;
        while (true) {
          if (FS.isRoot(node)) {
            var mount = node.mount.mountpoint;
            if (!path) return mount;
            return mount[mount.length-1] !== '/' ? mount + '/' + path : mount + path;
          }
          path = path ? node.name + '/' + path : node.name;
          node = node.parent;
        }
      },hashName:(parentid, name) => {
        var hash = 0;
  
        for (var i = 0; i < name.length; i++) {
          hash = ((hash << 5) - hash + name.charCodeAt(i)) | 0;
        }
        return ((parentid + hash) >>> 0) % FS.nameTable.length;
      },hashAddNode:(node) => {
        var hash = FS.hashName(node.parent.id, node.name);
        node.name_next = FS.nameTable[hash];
        FS.nameTable[hash] = node;
      },hashRemoveNode:(node) => {
        var hash = FS.hashName(node.parent.id, node.name);
        if (FS.nameTable[hash] === node) {
          FS.nameTable[hash] = node.name_next;
        } else {
          var current = FS.nameTable[hash];
          while (current) {
            if (current.name_next === node) {
              current.name_next = node.name_next;
              break;
            }
            current = current.name_next;
          }
        }
      },lookupNode:(parent, name) => {
        var errCode = FS.mayLookup(parent);
        if (errCode) {
          throw new FS.ErrnoError(errCode, parent);
        }
        var hash = FS.hashName(parent.id, name);
        for (var node = FS.nameTable[hash]; node; node = node.name_next) {
          var nodeName = node.name;
          if (node.parent.id === parent.id && nodeName === name) {
            return node;
          }
        }
        // if we failed to find it in the cache, call into the VFS
        return FS.lookup(parent, name);
      },createNode:(parent, name, mode, rdev) => {
        var node = new FS.FSNode(parent, name, mode, rdev);
  
        FS.hashAddNode(node);
  
        return node;
      },destroyNode:(node) => {
        FS.hashRemoveNode(node);
      },isRoot:(node) => {
        return node === node.parent;
      },isMountpoint:(node) => {
        return !!node.mounted;
      },isFile:(mode) => {
        return (mode & 61440) === 32768;
      },isDir:(mode) => {
        return (mode & 61440) === 16384;
      },isLink:(mode) => {
        return (mode & 61440) === 40960;
      },isChrdev:(mode) => {
        return (mode & 61440) === 8192;
      },isBlkdev:(mode) => {
        return (mode & 61440) === 24576;
      },isFIFO:(mode) => {
        return (mode & 61440) === 4096;
      },isSocket:(mode) => {
        return (mode & 49152) === 49152;
      },flagModes:{"r":0,"r+":2,"w":577,"w+":578,"a":1089,"a+":1090},modeStringToFlags:(str) => {
        var flags = FS.flagModes[str];
        if (typeof flags == 'undefined') {
          throw new Error('Unknown file open mode: ' + str);
        }
        return flags;
      },flagsToPermissionString:(flag) => {
        var perms = ['r', 'w', 'rw'][flag & 3];
        if ((flag & 512)) {
          perms += 'w';
        }
        return perms;
      },nodePermissions:(node, perms) => {
        if (FS.ignorePermissions) {
          return 0;
        }
        // return 0 if any user, group or owner bits are set.
        if (perms.includes('r') && !(node.mode & 292)) {
          return 2;
        } else if (perms.includes('w') && !(node.mode & 146)) {
          return 2;
        } else if (perms.includes('x') && !(node.mode & 73)) {
          return 2;
        }
        return 0;
      },mayLookup:(dir) => {
        var errCode = FS.nodePermissions(dir, 'x');
        if (errCode) return errCode;
        if (!dir.node_ops.lookup) return 2;
        return 0;
      },mayCreate:(dir, name) => {
        try {
          var node = FS.lookupNode(dir, name);
          return 20;
        } catch (e) {
        }
        return FS.nodePermissions(dir, 'wx');
      },mayDelete:(dir, name, isdir) => {
        var node;
        try {
          node = FS.lookupNode(dir, name);
        } catch (e) {
          return e.errno;
        }
        var errCode = FS.nodePermissions(dir, 'wx');
        if (errCode) {
          return errCode;
        }
        if (isdir) {
          if (!FS.isDir(node.mode)) {
            return 54;
          }
          if (FS.isRoot(node) || FS.getPath(node) === FS.cwd()) {
            return 10;
          }
        } else {
          if (FS.isDir(node.mode)) {
            return 31;
          }
        }
        return 0;
      },mayOpen:(node, flags) => {
        if (!node) {
          return 44;
        }
        if (FS.isLink(node.mode)) {
          return 32;
        } else if (FS.isDir(node.mode)) {
          if (FS.flagsToPermissionString(flags) !== 'r' || // opening for write
              (flags & 512)) { // TODO: check for O_SEARCH? (== search for dir only)
            return 31;
          }
        }
        return FS.nodePermissions(node, FS.flagsToPermissionString(flags));
      },MAX_OPEN_FDS:4096,nextfd:(fd_start = 0, fd_end = FS.MAX_OPEN_FDS) => {
        for (var fd = fd_start; fd <= fd_end; fd++) {
          if (!FS.streams[fd]) {
            return fd;
          }
        }
        throw new FS.ErrnoError(33);
      },getStream:(fd) => FS.streams[fd],createStream:(stream, fd_start, fd_end) => {
        if (!FS.FSStream) {
          FS.FSStream = /** @constructor */ function() {
            this.shared = { };
          };
          FS.FSStream.prototype = {
            object: {
              get: function() { return this.node; },
              set: function(val) { this.node = val; }
            },
            isRead: {
              get: function() { return (this.flags & 2097155) !== 1; }
            },
            isWrite: {
              get: function() { return (this.flags & 2097155) !== 0; }
            },
            isAppend: {
              get: function() { return (this.flags & 1024); }
            },
            flags: {
              get: function() { return this.shared.flags; },
              set: function(val) { this.shared.flags = val; },
            },
            position : {
              get function() { return this.shared.position; },
              set: function(val) { this.shared.position = val; },
            },
          };
        }
        // clone it, so we can return an instance of FSStream
        stream = Object.assign(new FS.FSStream(), stream);
        var fd = FS.nextfd(fd_start, fd_end);
        stream.fd = fd;
        FS.streams[fd] = stream;
        return stream;
      },closeStream:(fd) => {
        FS.streams[fd] = null;
      },chrdev_stream_ops:{open:(stream) => {
          var device = FS.getDevice(stream.node.rdev);
          // override node's stream ops with the device's
          stream.stream_ops = device.stream_ops;
          // forward the open call
          if (stream.stream_ops.open) {
            stream.stream_ops.open(stream);
          }
        },llseek:() => {
          throw new FS.ErrnoError(70);
        }},major:(dev) => ((dev) >> 8),minor:(dev) => ((dev) & 0xff),makedev:(ma, mi) => ((ma) << 8 | (mi)),registerDevice:(dev, ops) => {
        FS.devices[dev] = { stream_ops: ops };
      },getDevice:(dev) => FS.devices[dev],getMounts:(mount) => {
        var mounts = [];
        var check = [mount];
  
        while (check.length) {
          var m = check.pop();
  
          mounts.push(m);
  
          check.push.apply(check, m.mounts);
        }
  
        return mounts;
      },syncfs:(populate, callback) => {
        if (typeof populate == 'function') {
          callback = populate;
          populate = false;
        }
  
        FS.syncFSRequests++;
  
        if (FS.syncFSRequests > 1) {
          err('warning: ' + FS.syncFSRequests + ' FS.syncfs operations in flight at once, probably just doing extra work');
        }
  
        var mounts = FS.getMounts(FS.root.mount);
        var completed = 0;
  
        function doCallback(errCode) {
          FS.syncFSRequests--;
          return callback(errCode);
        }
  
        function done(errCode) {
          if (errCode) {
            if (!done.errored) {
              done.errored = true;
              return doCallback(errCode);
            }
            return;
          }
          if (++completed >= mounts.length) {
            doCallback(null);
          }
        };
  
        // sync all mounts
        mounts.forEach((mount) => {
          if (!mount.type.syncfs) {
            return done(null);
          }
          mount.type.syncfs(mount, populate, done);
        });
      },mount:(type, opts, mountpoint) => {
        var root = mountpoint === '/';
        var pseudo = !mountpoint;
        var node;
  
        if (root && FS.root) {
          throw new FS.ErrnoError(10);
        } else if (!root && !pseudo) {
          var lookup = FS.lookupPath(mountpoint, { follow_mount: false });
  
          mountpoint = lookup.path;  // use the absolute path
          node = lookup.node;
  
          if (FS.isMountpoint(node)) {
            throw new FS.ErrnoError(10);
          }
  
          if (!FS.isDir(node.mode)) {
            throw new FS.ErrnoError(54);
          }
        }
  
        var mount = {
          type: type,
          opts: opts,
          mountpoint: mountpoint,
          mounts: []
        };
  
        // create a root node for the fs
        var mountRoot = type.mount(mount);
        mountRoot.mount = mount;
        mount.root = mountRoot;
  
        if (root) {
          FS.root = mountRoot;
        } else if (node) {
          // set as a mountpoint
          node.mounted = mount;
  
          // add the new mount to the current mount's children
          if (node.mount) {
            node.mount.mounts.push(mount);
          }
        }
  
        return mountRoot;
      },unmount:(mountpoint) => {
        var lookup = FS.lookupPath(mountpoint, { follow_mount: false });
  
        if (!FS.isMountpoint(lookup.node)) {
          throw new FS.ErrnoError(28);
        }
  
        // destroy the nodes for this mount, and all its child mounts
        var node = lookup.node;
        var mount = node.mounted;
        var mounts = FS.getMounts(mount);
  
        Object.keys(FS.nameTable).forEach((hash) => {
          var current = FS.nameTable[hash];
  
          while (current) {
            var next = current.name_next;
  
            if (mounts.includes(current.mount)) {
              FS.destroyNode(current);
            }
  
            current = next;
          }
        });
  
        // no longer a mountpoint
        node.mounted = null;
  
        // remove this mount from the child mounts
        var idx = node.mount.mounts.indexOf(mount);
        node.mount.mounts.splice(idx, 1);
      },lookup:(parent, name) => {
        return parent.node_ops.lookup(parent, name);
      },mknod:(path, mode, dev) => {
        var lookup = FS.lookupPath(path, { parent: true });
        var parent = lookup.node;
        var name = PATH.basename(path);
        if (!name || name === '.' || name === '..') {
          throw new FS.ErrnoError(28);
        }
        var errCode = FS.mayCreate(parent, name);
        if (errCode) {
          throw new FS.ErrnoError(errCode);
        }
        if (!parent.node_ops.mknod) {
          throw new FS.ErrnoError(63);
        }
        return parent.node_ops.mknod(parent, name, mode, dev);
      },create:(path, mode) => {
        mode = mode !== undefined ? mode : 438 /* 0666 */;
        mode &= 4095;
        mode |= 32768;
        return FS.mknod(path, mode, 0);
      },mkdir:(path, mode) => {
        mode = mode !== undefined ? mode : 511 /* 0777 */;
        mode &= 511 | 512;
        mode |= 16384;
        return FS.mknod(path, mode, 0);
      },mkdirTree:(path, mode) => {
        var dirs = path.split('/');
        var d = '';
        for (var i = 0; i < dirs.length; ++i) {
          if (!dirs[i]) continue;
          d += '/' + dirs[i];
          try {
            FS.mkdir(d, mode);
          } catch(e) {
            if (e.errno != 20) throw e;
          }
        }
      },mkdev:(path, mode, dev) => {
        if (typeof dev == 'undefined') {
          dev = mode;
          mode = 438 /* 0666 */;
        }
        mode |= 8192;
        return FS.mknod(path, mode, dev);
      },symlink:(oldpath, newpath) => {
        if (!PATH_FS.resolve(oldpath)) {
          throw new FS.ErrnoError(44);
        }
        var lookup = FS.lookupPath(newpath, { parent: true });
        var parent = lookup.node;
        if (!parent) {
          throw new FS.ErrnoError(44);
        }
        var newname = PATH.basename(newpath);
        var errCode = FS.mayCreate(parent, newname);
        if (errCode) {
          throw new FS.ErrnoError(errCode);
        }
        if (!parent.node_ops.symlink) {
          throw new FS.ErrnoError(63);
        }
        return parent.node_ops.symlink(parent, newname, oldpath);
      },rename:(old_path, new_path) => {
        var old_dirname = PATH.dirname(old_path);
        var new_dirname = PATH.dirname(new_path);
        var old_name = PATH.basename(old_path);
        var new_name = PATH.basename(new_path);
        // parents must exist
        var lookup, old_dir, new_dir;
  
        // let the errors from non existant directories percolate up
        lookup = FS.lookupPath(old_path, { parent: true });
        old_dir = lookup.node;
        lookup = FS.lookupPath(new_path, { parent: true });
        new_dir = lookup.node;
  
        if (!old_dir || !new_dir) throw new FS.ErrnoError(44);
        // need to be part of the same mount
        if (old_dir.mount !== new_dir.mount) {
          throw new FS.ErrnoError(75);
        }
        // source must exist
        var old_node = FS.lookupNode(old_dir, old_name);
        // old path should not be an ancestor of the new path
        var relative = PATH_FS.relative(old_path, new_dirname);
        if (relative.charAt(0) !== '.') {
          throw new FS.ErrnoError(28);
        }
        // new path should not be an ancestor of the old path
        relative = PATH_FS.relative(new_path, old_dirname);
        if (relative.charAt(0) !== '.') {
          throw new FS.ErrnoError(55);
        }
        // see if the new path already exists
        var new_node;
        try {
          new_node = FS.lookupNode(new_dir, new_name);
        } catch (e) {
          // not fatal
        }
        // early out if nothing needs to change
        if (old_node === new_node) {
          return;
        }
        // we'll need to delete the old entry
        var isdir = FS.isDir(old_node.mode);
        var errCode = FS.mayDelete(old_dir, old_name, isdir);
        if (errCode) {
          throw new FS.ErrnoError(errCode);
        }
        // need delete permissions if we'll be overwriting.
        // need create permissions if new doesn't already exist.
        errCode = new_node ?
          FS.mayDelete(new_dir, new_name, isdir) :
          FS.mayCreate(new_dir, new_name);
        if (errCode) {
          throw new FS.ErrnoError(errCode);
        }
        if (!old_dir.node_ops.rename) {
          throw new FS.ErrnoError(63);
        }
        if (FS.isMountpoint(old_node) || (new_node && FS.isMountpoint(new_node))) {
          throw new FS.ErrnoError(10);
        }
        // if we are going to change the parent, check write permissions
        if (new_dir !== old_dir) {
          errCode = FS.nodePermissions(old_dir, 'w');
          if (errCode) {
            throw new FS.ErrnoError(errCode);
          }
        }
        // remove the node from the lookup hash
        FS.hashRemoveNode(old_node);
        // do the underlying fs rename
        try {
          old_dir.node_ops.rename(old_node, new_dir, new_name);
        } catch (e) {
          throw e;
        } finally {
          // add the node back to the hash (in case node_ops.rename
          // changed its name)
          FS.hashAddNode(old_node);
        }
      },rmdir:(path) => {
        var lookup = FS.lookupPath(path, { parent: true });
        var parent = lookup.node;
        var name = PATH.basename(path);
        var node = FS.lookupNode(parent, name);
        var errCode = FS.mayDelete(parent, name, true);
        if (errCode) {
          throw new FS.ErrnoError(errCode);
        }
        if (!parent.node_ops.rmdir) {
          throw new FS.ErrnoError(63);
        }
        if (FS.isMountpoint(node)) {
          throw new FS.ErrnoError(10);
        }
        parent.node_ops.rmdir(parent, name);
        FS.destroyNode(node);
      },readdir:(path) => {
        var lookup = FS.lookupPath(path, { follow: true });
        var node = lookup.node;
        if (!node.node_ops.readdir) {
          throw new FS.ErrnoError(54);
        }
        return node.node_ops.readdir(node);
      },unlink:(path) => {
        var lookup = FS.lookupPath(path, { parent: true });
        var parent = lookup.node;
        if (!parent) {
          throw new FS.ErrnoError(44);
        }
        var name = PATH.basename(path);
        var node = FS.lookupNode(parent, name);
        var errCode = FS.mayDelete(parent, name, false);
        if (errCode) {
          // According to POSIX, we should map EISDIR to EPERM, but
          // we instead do what Linux does (and we must, as we use
          // the musl linux libc).
          throw new FS.ErrnoError(errCode);
        }
        if (!parent.node_ops.unlink) {
          throw new FS.ErrnoError(63);
        }
        if (FS.isMountpoint(node)) {
          throw new FS.ErrnoError(10);
        }
        parent.node_ops.unlink(parent, name);
        FS.destroyNode(node);
      },readlink:(path) => {
        var lookup = FS.lookupPath(path);
        var link = lookup.node;
        if (!link) {
          throw new FS.ErrnoError(44);
        }
        if (!link.node_ops.readlink) {
          throw new FS.ErrnoError(28);
        }
        return PATH_FS.resolve(FS.getPath(link.parent), link.node_ops.readlink(link));
      },stat:(path, dontFollow) => {
        var lookup = FS.lookupPath(path, { follow: !dontFollow });
        var node = lookup.node;
        if (!node) {
          throw new FS.ErrnoError(44);
        }
        if (!node.node_ops.getattr) {
          throw new FS.ErrnoError(63);
        }
        return node.node_ops.getattr(node);
      },lstat:(path) => {
        return FS.stat(path, true);
      },chmod:(path, mode, dontFollow) => {
        var node;
        if (typeof path == 'string') {
          var lookup = FS.lookupPath(path, { follow: !dontFollow });
          node = lookup.node;
        } else {
          node = path;
        }
        if (!node.node_ops.setattr) {
          throw new FS.ErrnoError(63);
        }
        node.node_ops.setattr(node, {
          mode: (mode & 4095) | (node.mode & ~4095),
          timestamp: Date.now()
        });
      },lchmod:(path, mode) => {
        FS.chmod(path, mode, true);
      },fchmod:(fd, mode) => {
        var stream = FS.getStream(fd);
        if (!stream) {
          throw new FS.ErrnoError(8);
        }
        FS.chmod(stream.node, mode);
      },chown:(path, uid, gid, dontFollow) => {
        var node;
        if (typeof path == 'string') {
          var lookup = FS.lookupPath(path, { follow: !dontFollow });
          node = lookup.node;
        } else {
          node = path;
        }
        if (!node.node_ops.setattr) {
          throw new FS.ErrnoError(63);
        }
        node.node_ops.setattr(node, {
          timestamp: Date.now()
          // we ignore the uid / gid for now
        });
      },lchown:(path, uid, gid) => {
        FS.chown(path, uid, gid, true);
      },fchown:(fd, uid, gid) => {
        var stream = FS.getStream(fd);
        if (!stream) {
          throw new FS.ErrnoError(8);
        }
        FS.chown(stream.node, uid, gid);
      },truncate:(path, len) => {
        if (len < 0) {
          throw new FS.ErrnoError(28);
        }
        var node;
        if (typeof path == 'string') {
          var lookup = FS.lookupPath(path, { follow: true });
          node = lookup.node;
        } else {
          node = path;
        }
        if (!node.node_ops.setattr) {
          throw new FS.ErrnoError(63);
        }
        if (FS.isDir(node.mode)) {
          throw new FS.ErrnoError(31);
        }
        if (!FS.isFile(node.mode)) {
          throw new FS.ErrnoError(28);
        }
        var errCode = FS.nodePermissions(node, 'w');
        if (errCode) {
          throw new FS.ErrnoError(errCode);
        }
        node.node_ops.setattr(node, {
          size: len,
          timestamp: Date.now()
        });
      },ftruncate:(fd, len) => {
        var stream = FS.getStream(fd);
        if (!stream) {
          throw new FS.ErrnoError(8);
        }
        if ((stream.flags & 2097155) === 0) {
          throw new FS.ErrnoError(28);
        }
        FS.truncate(stream.node, len);
      },utime:(path, atime, mtime) => {
        var lookup = FS.lookupPath(path, { follow: true });
        var node = lookup.node;
        node.node_ops.setattr(node, {
          timestamp: Math.max(atime, mtime)
        });
      },open:(path, flags, mode) => {
        if (path === "") {
          throw new FS.ErrnoError(44);
        }
        flags = typeof flags == 'string' ? FS.modeStringToFlags(flags) : flags;
        mode = typeof mode == 'undefined' ? 438 /* 0666 */ : mode;
        if ((flags & 64)) {
          mode = (mode & 4095) | 32768;
        } else {
          mode = 0;
        }
        var node;
        if (typeof path == 'object') {
          node = path;
        } else {
          path = PATH.normalize(path);
          try {
            var lookup = FS.lookupPath(path, {
              follow: !(flags & 131072)
            });
            node = lookup.node;
          } catch (e) {
            // ignore
          }
        }
        // perhaps we need to create the node
        var created = false;
        if ((flags & 64)) {
          if (node) {
            // if O_CREAT and O_EXCL are set, error out if the node already exists
            if ((flags & 128)) {
              throw new FS.ErrnoError(20);
            }
          } else {
            // node doesn't exist, try to create it
            node = FS.mknod(path, mode, 0);
            created = true;
          }
        }
        if (!node) {
          throw new FS.ErrnoError(44);
        }
        // can't truncate a device
        if (FS.isChrdev(node.mode)) {
          flags &= ~512;
        }
        // if asked only for a directory, then this must be one
        if ((flags & 65536) && !FS.isDir(node.mode)) {
          throw new FS.ErrnoError(54);
        }
        // check permissions, if this is not a file we just created now (it is ok to
        // create and write to a file with read-only permissions; it is read-only
        // for later use)
        if (!created) {
          var errCode = FS.mayOpen(node, flags);
          if (errCode) {
            throw new FS.ErrnoError(errCode);
          }
        }
        // do truncation if necessary
        if ((flags & 512) && !created) {
          FS.truncate(node, 0);
        }
        // we've already handled these, don't pass down to the underlying vfs
        flags &= ~(128 | 512 | 131072);
  
        // register the stream with the filesystem
        var stream = FS.createStream({
          node: node,
          path: FS.getPath(node),  // we want the absolute path to the node
          flags: flags,
          seekable: true,
          position: 0,
          stream_ops: node.stream_ops,
          // used by the file family libc calls (fopen, fwrite, ferror, etc.)
          ungotten: [],
          error: false
        });
        // call the new stream's open function
        if (stream.stream_ops.open) {
          stream.stream_ops.open(stream);
        }
        if (Module['logReadFiles'] && !(flags & 1)) {
          if (!FS.readFiles) FS.readFiles = {};
          if (!(path in FS.readFiles)) {
            FS.readFiles[path] = 1;
          }
        }
        return stream;
      },close:(stream) => {
        if (FS.isClosed(stream)) {
          throw new FS.ErrnoError(8);
        }
        if (stream.getdents) stream.getdents = null; // free readdir state
        try {
          if (stream.stream_ops.close) {
            stream.stream_ops.close(stream);
          }
        } catch (e) {
          throw e;
        } finally {
          FS.closeStream(stream.fd);
        }
        stream.fd = null;
      },isClosed:(stream) => {
        return stream.fd === null;
      },llseek:(stream, offset, whence) => {
        if (FS.isClosed(stream)) {
          throw new FS.ErrnoError(8);
        }
        if (!stream.seekable || !stream.stream_ops.llseek) {
          throw new FS.ErrnoError(70);
        }
        if (whence != 0 && whence != 1 && whence != 2) {
          throw new FS.ErrnoError(28);
        }
        stream.position = stream.stream_ops.llseek(stream, offset, whence);
        stream.ungotten = [];
        return stream.position;
      },read:(stream, buffer, offset, length, position) => {
        if (length < 0 || position < 0) {
          throw new FS.ErrnoError(28);
        }
        if (FS.isClosed(stream)) {
          throw new FS.ErrnoError(8);
        }
        if ((stream.flags & 2097155) === 1) {
          throw new FS.ErrnoError(8);
        }
        if (FS.isDir(stream.node.mode)) {
          throw new FS.ErrnoError(31);
        }
        if (!stream.stream_ops.read) {
          throw new FS.ErrnoError(28);
        }
        var seeking = typeof position != 'undefined';
        if (!seeking) {
          position = stream.position;
        } else if (!stream.seekable) {
          throw new FS.ErrnoError(70);
        }
        var bytesRead = stream.stream_ops.read(stream, buffer, offset, length, position);
        if (!seeking) stream.position += bytesRead;
        return bytesRead;
      },write:(stream, buffer, offset, length, position, canOwn) => {
        if (length < 0 || position < 0) {
          throw new FS.ErrnoError(28);
        }
        if (FS.isClosed(stream)) {
          throw new FS.ErrnoError(8);
        }
        if ((stream.flags & 2097155) === 0) {
          throw new FS.ErrnoError(8);
        }
        if (FS.isDir(stream.node.mode)) {
          throw new FS.ErrnoError(31);
        }
        if (!stream.stream_ops.write) {
          throw new FS.ErrnoError(28);
        }
        if (stream.seekable && stream.flags & 1024) {
          // seek to the end before writing in append mode
          FS.llseek(stream, 0, 2);
        }
        var seeking = typeof position != 'undefined';
        if (!seeking) {
          position = stream.position;
        } else if (!stream.seekable) {
          throw new FS.ErrnoError(70);
        }
        var bytesWritten = stream.stream_ops.write(stream, buffer, offset, length, position, canOwn);
        if (!seeking) stream.position += bytesWritten;
        return bytesWritten;
      },allocate:(stream, offset, length) => {
        if (FS.isClosed(stream)) {
          throw new FS.ErrnoError(8);
        }
        if (offset < 0 || length <= 0) {
          throw new FS.ErrnoError(28);
        }
        if ((stream.flags & 2097155) === 0) {
          throw new FS.ErrnoError(8);
        }
        if (!FS.isFile(stream.node.mode) && !FS.isDir(stream.node.mode)) {
          throw new FS.ErrnoError(43);
        }
        if (!stream.stream_ops.allocate) {
          throw new FS.ErrnoError(138);
        }
        stream.stream_ops.allocate(stream, offset, length);
      },mmap:(stream, length, position, prot, flags) => {
        // User requests writing to file (prot & PROT_WRITE != 0).
        // Checking if we have permissions to write to the file unless
        // MAP_PRIVATE flag is set. According to POSIX spec it is possible
        // to write to file opened in read-only mode with MAP_PRIVATE flag,
        // as all modifications will be visible only in the memory of
        // the current process.
        if ((prot & 2) !== 0
            && (flags & 2) === 0
            && (stream.flags & 2097155) !== 2) {
          throw new FS.ErrnoError(2);
        }
        if ((stream.flags & 2097155) === 1) {
          throw new FS.ErrnoError(2);
        }
        if (!stream.stream_ops.mmap) {
          throw new FS.ErrnoError(43);
        }
        return stream.stream_ops.mmap(stream, length, position, prot, flags);
      },msync:(stream, buffer, offset, length, mmapFlags) => {
        if (!stream || !stream.stream_ops.msync) {
          return 0;
        }
        return stream.stream_ops.msync(stream, buffer, offset, length, mmapFlags);
      },munmap:(stream) => 0,ioctl:(stream, cmd, arg) => {
        if (!stream.stream_ops.ioctl) {
          throw new FS.ErrnoError(59);
        }
        return stream.stream_ops.ioctl(stream, cmd, arg);
      },readFile:(path, opts = {}) => {
        opts.flags = opts.flags || 0;
        opts.encoding = opts.encoding || 'binary';
        if (opts.encoding !== 'utf8' && opts.encoding !== 'binary') {
          throw new Error('Invalid encoding type "' + opts.encoding + '"');
        }
        var ret;
        var stream = FS.open(path, opts.flags);
        var stat = FS.stat(path);
        var length = stat.size;
        var buf = new Uint8Array(length);
        FS.read(stream, buf, 0, length, 0);
        if (opts.encoding === 'utf8') {
          ret = UTF8ArrayToString(buf, 0);
        } else if (opts.encoding === 'binary') {
          ret = buf;
        }
        FS.close(stream);
        return ret;
      },writeFile:(path, data, opts = {}) => {
        opts.flags = opts.flags || 577;
        var stream = FS.open(path, opts.flags, opts.mode);
        if (typeof data == 'string') {
          var buf = new Uint8Array(lengthBytesUTF8(data)+1);
          var actualNumBytes = stringToUTF8Array(data, buf, 0, buf.length);
          FS.write(stream, buf, 0, actualNumBytes, undefined, opts.canOwn);
        } else if (ArrayBuffer.isView(data)) {
          FS.write(stream, data, 0, data.byteLength, undefined, opts.canOwn);
        } else {
          throw new Error('Unsupported data type');
        }
        FS.close(stream);
      },cwd:() => FS.currentPath,chdir:(path) => {
        var lookup = FS.lookupPath(path, { follow: true });
        if (lookup.node === null) {
          throw new FS.ErrnoError(44);
        }
        if (!FS.isDir(lookup.node.mode)) {
          throw new FS.ErrnoError(54);
        }
        var errCode = FS.nodePermissions(lookup.node, 'x');
        if (errCode) {
          throw new FS.ErrnoError(errCode);
        }
        FS.currentPath = lookup.path;
      },createDefaultDirectories:() => {
        FS.mkdir('/tmp');
        FS.mkdir('/home');
        FS.mkdir('/home/web_user');
      },createDefaultDevices:() => {
        // create /dev
        FS.mkdir('/dev');
        // setup /dev/null
        FS.registerDevice(FS.makedev(1, 3), {
          read: () => 0,
          write: (stream, buffer, offset, length, pos) => length,
        });
        FS.mkdev('/dev/null', FS.makedev(1, 3));
        // setup /dev/tty and /dev/tty1
        // stderr needs to print output using err() rather than out()
        // so we register a second tty just for it.
        TTY.register(FS.makedev(5, 0), TTY.default_tty_ops);
        TTY.register(FS.makedev(6, 0), TTY.default_tty1_ops);
        FS.mkdev('/dev/tty', FS.makedev(5, 0));
        FS.mkdev('/dev/tty1', FS.makedev(6, 0));
        // setup /dev/[u]random
        var random_device = getRandomDevice();
        FS.createDevice('/dev', 'random', random_device);
        FS.createDevice('/dev', 'urandom', random_device);
        // we're not going to emulate the actual shm device,
        // just create the tmp dirs that reside in it commonly
        FS.mkdir('/dev/shm');
        FS.mkdir('/dev/shm/tmp');
      },createSpecialDirectories:() => {
        // create /proc/self/fd which allows /proc/self/fd/6 => readlink gives the
        // name of the stream for fd 6 (see test_unistd_ttyname)
        FS.mkdir('/proc');
        var proc_self = FS.mkdir('/proc/self');
        FS.mkdir('/proc/self/fd');
        FS.mount({
          mount: () => {
            var node = FS.createNode(proc_self, 'fd', 16384 | 511 /* 0777 */, 73);
            node.node_ops = {
              lookup: (parent, name) => {
                var fd = +name;
                var stream = FS.getStream(fd);
                if (!stream) throw new FS.ErrnoError(8);
                var ret = {
                  parent: null,
                  mount: { mountpoint: 'fake' },
                  node_ops: { readlink: () => stream.path },
                };
                ret.parent = ret; // make it look like a simple root node
                return ret;
              }
            };
            return node;
          }
        }, {}, '/proc/self/fd');
      },createStandardStreams:() => {
        // TODO deprecate the old functionality of a single
        // input / output callback and that utilizes FS.createDevice
        // and instead require a unique set of stream ops
  
        // by default, we symlink the standard streams to the
        // default tty devices. however, if the standard streams
        // have been overwritten we create a unique device for
        // them instead.
        if (Module['stdin']) {
          FS.createDevice('/dev', 'stdin', Module['stdin']);
        } else {
          FS.symlink('/dev/tty', '/dev/stdin');
        }
        if (Module['stdout']) {
          FS.createDevice('/dev', 'stdout', null, Module['stdout']);
        } else {
          FS.symlink('/dev/tty', '/dev/stdout');
        }
        if (Module['stderr']) {
          FS.createDevice('/dev', 'stderr', null, Module['stderr']);
        } else {
          FS.symlink('/dev/tty1', '/dev/stderr');
        }
  
        // open default streams for the stdin, stdout and stderr devices
        var stdin = FS.open('/dev/stdin', 0);
        var stdout = FS.open('/dev/stdout', 1);
        var stderr = FS.open('/dev/stderr', 1);
      },ensureErrnoError:() => {
        if (FS.ErrnoError) return;
        FS.ErrnoError = /** @this{Object} */ function ErrnoError(errno, node) {
          this.node = node;
          this.setErrno = /** @this{Object} */ function(errno) {
            this.errno = errno;
          };
          this.setErrno(errno);
          this.message = 'FS error';
  
        };
        FS.ErrnoError.prototype = new Error();
        FS.ErrnoError.prototype.constructor = FS.ErrnoError;
        // Some errors may happen quite a bit, to avoid overhead we reuse them (and suffer a lack of stack info)
        [44].forEach((code) => {
          FS.genericErrors[code] = new FS.ErrnoError(code);
          FS.genericErrors[code].stack = '<generic error, no stack>';
        });
      },staticInit:() => {
        FS.ensureErrnoError();
  
        FS.nameTable = new Array(4096);
  
        FS.mount(MEMFS, {}, '/');
  
        FS.createDefaultDirectories();
        FS.createDefaultDevices();
        FS.createSpecialDirectories();
  
        FS.filesystems = {
          'MEMFS': MEMFS,
        };
      },init:(input, output, error) => {
        FS.init.initialized = true;
  
        FS.ensureErrnoError();
  
        // Allow Module.stdin etc. to provide defaults, if none explicitly passed to us here
        Module['stdin'] = input || Module['stdin'];
        Module['stdout'] = output || Module['stdout'];
        Module['stderr'] = error || Module['stderr'];
  
        FS.createStandardStreams();
      },quit:() => {
        FS.init.initialized = false;
        // force-flush all streams, so we get musl std streams printed out
        // close all of our streams
        for (var i = 0; i < FS.streams.length; i++) {
          var stream = FS.streams[i];
          if (!stream) {
            continue;
          }
          FS.close(stream);
        }
      },getMode:(canRead, canWrite) => {
        var mode = 0;
        if (canRead) mode |= 292 | 73;
        if (canWrite) mode |= 146;
        return mode;
      },findObject:(path, dontResolveLastLink) => {
        var ret = FS.analyzePath(path, dontResolveLastLink);
        if (ret.exists) {
          return ret.object;
        } else {
          return null;
        }
      },analyzePath:(path, dontResolveLastLink) => {
        // operate from within the context of the symlink's target
        try {
          var lookup = FS.lookupPath(path, { follow: !dontResolveLastLink });
          path = lookup.path;
        } catch (e) {
        }
        var ret = {
          isRoot: false, exists: false, error: 0, name: null, path: null, object: null,
          parentExists: false, parentPath: null, parentObject: null
        };
        try {
          var lookup = FS.lookupPath(path, { parent: true });
          ret.parentExists = true;
          ret.parentPath = lookup.path;
          ret.parentObject = lookup.node;
          ret.name = PATH.basename(path);
          lookup = FS.lookupPath(path, { follow: !dontResolveLastLink });
          ret.exists = true;
          ret.path = lookup.path;
          ret.object = lookup.node;
          ret.name = lookup.node.name;
          ret.isRoot = lookup.path === '/';
        } catch (e) {
          ret.error = e.errno;
        };
        return ret;
      },createPath:(parent, path, canRead, canWrite) => {
        parent = typeof parent == 'string' ? parent : FS.getPath(parent);
        var parts = path.split('/').reverse();
        while (parts.length) {
          var part = parts.pop();
          if (!part) continue;
          var current = PATH.join2(parent, part);
          try {
            FS.mkdir(current);
          } catch (e) {
            // ignore EEXIST
          }
          parent = current;
        }
        return current;
      },createFile:(parent, name, properties, canRead, canWrite) => {
        var path = PATH.join2(typeof parent == 'string' ? parent : FS.getPath(parent), name);
        var mode = FS.getMode(canRead, canWrite);
        return FS.create(path, mode);
      },createDataFile:(parent, name, data, canRead, canWrite, canOwn) => {
        var path = name;
        if (parent) {
          parent = typeof parent == 'string' ? parent : FS.getPath(parent);
          path = name ? PATH.join2(parent, name) : parent;
        }
        var mode = FS.getMode(canRead, canWrite);
        var node = FS.create(path, mode);
        if (data) {
          if (typeof data == 'string') {
            var arr = new Array(data.length);
            for (var i = 0, len = data.length; i < len; ++i) arr[i] = data.charCodeAt(i);
            data = arr;
          }
          // make sure we can write to the file
          FS.chmod(node, mode | 146);
          var stream = FS.open(node, 577);
          FS.write(stream, data, 0, data.length, 0, canOwn);
          FS.close(stream);
          FS.chmod(node, mode);
        }
        return node;
      },createDevice:(parent, name, input, output) => {
        var path = PATH.join2(typeof parent == 'string' ? parent : FS.getPath(parent), name);
        var mode = FS.getMode(!!input, !!output);
        if (!FS.createDevice.major) FS.createDevice.major = 64;
        var dev = FS.makedev(FS.createDevice.major++, 0);
        // Create a fake device that a set of stream ops to emulate
        // the old behavior.
        FS.registerDevice(dev, {
          open: (stream) => {
            stream.seekable = false;
          },
          close: (stream) => {
            // flush any pending line data
            if (output && output.buffer && output.buffer.length) {
              output(10);
            }
          },
          read: (stream, buffer, offset, length, pos /* ignored */) => {
            var bytesRead = 0;
            for (var i = 0; i < length; i++) {
              var result;
              try {
                result = input();
              } catch (e) {
                throw new FS.ErrnoError(29);
              }
              if (result === undefined && bytesRead === 0) {
                throw new FS.ErrnoError(6);
              }
              if (result === null || result === undefined) break;
              bytesRead++;
              buffer[offset+i] = result;
            }
            if (bytesRead) {
              stream.node.timestamp = Date.now();
            }
            return bytesRead;
          },
          write: (stream, buffer, offset, length, pos) => {
            for (var i = 0; i < length; i++) {
              try {
                output(buffer[offset+i]);
              } catch (e) {
                throw new FS.ErrnoError(29);
              }
            }
            if (length) {
              stream.node.timestamp = Date.now();
            }
            return i;
          }
        });
        return FS.mkdev(path, mode, dev);
      },forceLoadFile:(obj) => {
        if (obj.isDevice || obj.isFolder || obj.link || obj.contents) return true;
        if (typeof XMLHttpRequest != 'undefined') {
          throw new Error("Lazy loading should have been performed (contents set) in createLazyFile, but it was not. Lazy loading only works in web workers. Use --embed-file or --preload-file in emcc on the main thread.");
        } else if (read_) {
          // Command-line.
          try {
            // WARNING: Can't read binary files in V8's d8 or tracemonkey's js, as
            //          read() will try to parse UTF8.
            obj.contents = intArrayFromString(read_(obj.url), true);
            obj.usedBytes = obj.contents.length;
          } catch (e) {
            throw new FS.ErrnoError(29);
          }
        } else {
          throw new Error('Cannot load without read() or XMLHttpRequest.');
        }
      },createLazyFile:(parent, name, url, canRead, canWrite) => {
        // Lazy chunked Uint8Array (implements get and length from Uint8Array). Actual getting is abstracted away for eventual reuse.
        /** @constructor */
        function LazyUint8Array() {
          this.lengthKnown = false;
          this.chunks = []; // Loaded chunks. Index is the chunk number
        }
        LazyUint8Array.prototype.get = /** @this{Object} */ function LazyUint8Array_get(idx) {
          if (idx > this.length-1 || idx < 0) {
            return undefined;
          }
          var chunkOffset = idx % this.chunkSize;
          var chunkNum = (idx / this.chunkSize)|0;
          return this.getter(chunkNum)[chunkOffset];
        };
        LazyUint8Array.prototype.setDataGetter = function LazyUint8Array_setDataGetter(getter) {
          this.getter = getter;
        };
        LazyUint8Array.prototype.cacheLength = function LazyUint8Array_cacheLength() {
          // Find length
          var xhr = new XMLHttpRequest();
          xhr.open('HEAD', url, false);
          xhr.send(null);
          if (!(xhr.status >= 200 && xhr.status < 300 || xhr.status === 304)) throw new Error("Couldn't load " + url + ". Status: " + xhr.status);
          var datalength = Number(xhr.getResponseHeader("Content-length"));
          var header;
          var hasByteServing = (header = xhr.getResponseHeader("Accept-Ranges")) && header === "bytes";
          var usesGzip = (header = xhr.getResponseHeader("Content-Encoding")) && header === "gzip";
  
          var chunkSize = 1024*1024; // Chunk size in bytes
  
          if (!hasByteServing) chunkSize = datalength;
  
          // Function to get a range from the remote URL.
          var doXHR = (from, to) => {
            if (from > to) throw new Error("invalid range (" + from + ", " + to + ") or no bytes requested!");
            if (to > datalength-1) throw new Error("only " + datalength + " bytes available! programmer error!");
  
            // TODO: Use mozResponseArrayBuffer, responseStream, etc. if available.
            var xhr = new XMLHttpRequest();
            xhr.open('GET', url, false);
            if (datalength !== chunkSize) xhr.setRequestHeader("Range", "bytes=" + from + "-" + to);
  
            // Some hints to the browser that we want binary data.
            xhr.responseType = 'arraybuffer';
            if (xhr.overrideMimeType) {
              xhr.overrideMimeType('text/plain; charset=x-user-defined');
            }
  
            xhr.send(null);
            if (!(xhr.status >= 200 && xhr.status < 300 || xhr.status === 304)) throw new Error("Couldn't load " + url + ". Status: " + xhr.status);
            if (xhr.response !== undefined) {
              return new Uint8Array(/** @type{Array<number>} */(xhr.response || []));
            } else {
              return intArrayFromString(xhr.responseText || '', true);
            }
          };
          var lazyArray = this;
          lazyArray.setDataGetter((chunkNum) => {
            var start = chunkNum * chunkSize;
            var end = (chunkNum+1) * chunkSize - 1; // including this byte
            end = Math.min(end, datalength-1); // if datalength-1 is selected, this is the last block
            if (typeof lazyArray.chunks[chunkNum] == 'undefined') {
              lazyArray.chunks[chunkNum] = doXHR(start, end);
            }
            if (typeof lazyArray.chunks[chunkNum] == 'undefined') throw new Error('doXHR failed!');
            return lazyArray.chunks[chunkNum];
          });
  
          if (usesGzip || !datalength) {
            // if the server uses gzip or doesn't supply the length, we have to download the whole file to get the (uncompressed) length
            chunkSize = datalength = 1; // this will force getter(0)/doXHR do download the whole file
            datalength = this.getter(0).length;
            chunkSize = datalength;
            out("LazyFiles on gzip forces download of the whole file when length is accessed");
          }
  
          this._length = datalength;
          this._chunkSize = chunkSize;
          this.lengthKnown = true;
        };
        if (typeof XMLHttpRequest != 'undefined') {
          if (!ENVIRONMENT_IS_WORKER) throw 'Cannot do synchronous binary XHRs outside webworkers in modern browsers. Use --embed-file or --preload-file in emcc';
          var lazyArray = new LazyUint8Array();
          Object.defineProperties(lazyArray, {
            length: {
              get: /** @this{Object} */ function() {
                if (!this.lengthKnown) {
                  this.cacheLength();
                }
                return this._length;
              }
            },
            chunkSize: {
              get: /** @this{Object} */ function() {
                if (!this.lengthKnown) {
                  this.cacheLength();
                }
                return this._chunkSize;
              }
            }
          });
  
          var properties = { isDevice: false, contents: lazyArray };
        } else {
          var properties = { isDevice: false, url: url };
        }
  
        var node = FS.createFile(parent, name, properties, canRead, canWrite);
        // This is a total hack, but I want to get this lazy file code out of the
        // core of MEMFS. If we want to keep this lazy file concept I feel it should
        // be its own thin LAZYFS proxying calls to MEMFS.
        if (properties.contents) {
          node.contents = properties.contents;
        } else if (properties.url) {
          node.contents = null;
          node.url = properties.url;
        }
        // Add a function that defers querying the file size until it is asked the first time.
        Object.defineProperties(node, {
          usedBytes: {
            get: /** @this {FSNode} */ function() { return this.contents.length; }
          }
        });
        // override each stream op with one that tries to force load the lazy file first
        var stream_ops = {};
        var keys = Object.keys(node.stream_ops);
        keys.forEach((key) => {
          var fn = node.stream_ops[key];
          stream_ops[key] = function forceLoadLazyFile() {
            FS.forceLoadFile(node);
            return fn.apply(null, arguments);
          };
        });
        // use a custom read function
        stream_ops.read = (stream, buffer, offset, length, position) => {
          FS.forceLoadFile(node);
          var contents = stream.node.contents;
          if (position >= contents.length)
            return 0;
          var size = Math.min(contents.length - position, length);
          if (contents.slice) { // normal array
            for (var i = 0; i < size; i++) {
              buffer[offset + i] = contents[position + i];
            }
          } else {
            for (var i = 0; i < size; i++) { // LazyUint8Array from sync binary XHR
              buffer[offset + i] = contents.get(position + i);
            }
          }
          return size;
        };
        node.stream_ops = stream_ops;
        return node;
      },createPreloadedFile:(parent, name, url, canRead, canWrite, onload, onerror, dontCreateFile, canOwn, preFinish) => {
        // TODO we should allow people to just pass in a complete filename instead
        // of parent and name being that we just join them anyways
        var fullname = name ? PATH_FS.resolve(PATH.join2(parent, name)) : parent;
        var dep = getUniqueRunDependency('cp ' + fullname); // might have several active requests for the same fullname
        function processData(byteArray) {
          function finish(byteArray) {
            if (preFinish) preFinish();
            if (!dontCreateFile) {
              FS.createDataFile(parent, name, byteArray, canRead, canWrite, canOwn);
            }
            if (onload) onload();
            removeRunDependency(dep);
          }
          if (Browser.handledByPreloadPlugin(byteArray, fullname, finish, () => {
            if (onerror) onerror();
            removeRunDependency(dep);
          })) {
            return;
          }
          finish(byteArray);
        }
        addRunDependency(dep);
        if (typeof url == 'string') {
          asyncLoad(url, (byteArray) => processData(byteArray), onerror);
        } else {
          processData(url);
        }
      },indexedDB:() => {
        return window.indexedDB || window.mozIndexedDB || window.webkitIndexedDB || window.msIndexedDB;
      },DB_NAME:() => {
        return 'EM_FS_' + window.location.pathname;
      },DB_VERSION:20,DB_STORE_NAME:"FILE_DATA",saveFilesToDB:(paths, onload, onerror) => {
        onload = onload || (() => {});
        onerror = onerror || (() => {});
        var indexedDB = FS.indexedDB();
        try {
          var openRequest = indexedDB.open(FS.DB_NAME(), FS.DB_VERSION);
        } catch (e) {
          return onerror(e);
        }
        openRequest.onupgradeneeded = () => {
          out('creating db');
          var db = openRequest.result;
          db.createObjectStore(FS.DB_STORE_NAME);
        };
        openRequest.onsuccess = () => {
          var db = openRequest.result;
          var transaction = db.transaction([FS.DB_STORE_NAME], 'readwrite');
          var files = transaction.objectStore(FS.DB_STORE_NAME);
          var ok = 0, fail = 0, total = paths.length;
          function finish() {
            if (fail == 0) onload(); else onerror();
          }
          paths.forEach((path) => {
            var putRequest = files.put(FS.analyzePath(path).object.contents, path);
            putRequest.onsuccess = () => { ok++; if (ok + fail == total) finish() };
            putRequest.onerror = () => { fail++; if (ok + fail == total) finish() };
          });
          transaction.onerror = onerror;
        };
        openRequest.onerror = onerror;
      },loadFilesFromDB:(paths, onload, onerror) => {
        onload = onload || (() => {});
        onerror = onerror || (() => {});
        var indexedDB = FS.indexedDB();
        try {
          var openRequest = indexedDB.open(FS.DB_NAME(), FS.DB_VERSION);
        } catch (e) {
          return onerror(e);
        }
        openRequest.onupgradeneeded = onerror; // no database to load from
        openRequest.onsuccess = () => {
          var db = openRequest.result;
          try {
            var transaction = db.transaction([FS.DB_STORE_NAME], 'readonly');
          } catch(e) {
            onerror(e);
            return;
          }
          var files = transaction.objectStore(FS.DB_STORE_NAME);
          var ok = 0, fail = 0, total = paths.length;
          function finish() {
            if (fail == 0) onload(); else onerror();
          }
          paths.forEach((path) => {
            var getRequest = files.get(path);
            getRequest.onsuccess = () => {
              if (FS.analyzePath(path).exists) {
                FS.unlink(path);
              }
              FS.createDataFile(PATH.dirname(path), PATH.basename(path), getRequest.result, true, true, true);
              ok++;
              if (ok + fail == total) finish();
            };
            getRequest.onerror = () => { fail++; if (ok + fail == total) finish() };
          });
          transaction.onerror = onerror;
        };
        openRequest.onerror = onerror;
      }};
  var SOCKFS = {mount:function(mount) {
        // If Module['websocket'] has already been defined (e.g. for configuring
        // the subprotocol/url) use that, if not initialise it to a new object.
        Module['websocket'] = (Module['websocket'] && 
                               ('object' === typeof Module['websocket'])) ? Module['websocket'] : {};
  
        // Add the Event registration mechanism to the exported websocket configuration
        // object so we can register network callbacks from native JavaScript too.
        // For more documentation see system/include/emscripten/emscripten.h
        Module['websocket']._callbacks = {};
        Module['websocket']['on'] = /** @this{Object} */ function(event, callback) {
          if ('function' === typeof callback) {
            this._callbacks[event] = callback;
          }
          return this;
        };
  
        Module['websocket'].emit = /** @this{Object} */ function(event, param) {
          if ('function' === typeof this._callbacks[event]) {
            this._callbacks[event].call(this, param);
          }
        };
  
        // If debug is enabled register simple default logging callbacks for each Event.
  
        return FS.createNode(null, '/', 16384 | 511 /* 0777 */, 0);
      },createSocket:function(family, type, protocol) {
        type &= ~526336; // Some applications may pass it; it makes no sense for a single process.
        var streaming = type == 1;
        if (streaming && protocol && protocol != 6) {
          throw new FS.ErrnoError(66); // if SOCK_STREAM, must be tcp or 0.
        }
  
        // create our internal socket structure
        var sock = {
          family: family,
          type: type,
          protocol: protocol,
          server: null,
          error: null, // Used in getsockopt for SOL_SOCKET/SO_ERROR test
          peers: {},
          pending: [],
          recv_queue: [],
          sock_ops: SOCKFS.websocket_sock_ops
        };
  
        // create the filesystem node to store the socket structure
        var name = SOCKFS.nextname();
        var node = FS.createNode(SOCKFS.root, name, 49152, 0);
        node.sock = sock;
  
        // and the wrapping stream that enables library functions such
        // as read and write to indirectly interact with the socket
        var stream = FS.createStream({
          path: name,
          node: node,
          flags: 2,
          seekable: false,
          stream_ops: SOCKFS.stream_ops
        });
  
        // map the new stream to the socket structure (sockets have a 1:1
        // relationship with a stream)
        sock.stream = stream;
  
        return sock;
      },getSocket:function(fd) {
        var stream = FS.getStream(fd);
        if (!stream || !FS.isSocket(stream.node.mode)) {
          return null;
        }
        return stream.node.sock;
      },stream_ops:{poll:function(stream) {
          var sock = stream.node.sock;
          return sock.sock_ops.poll(sock);
        },ioctl:function(stream, request, varargs) {
          var sock = stream.node.sock;
          return sock.sock_ops.ioctl(sock, request, varargs);
        },read:function(stream, buffer, offset, length, position /* ignored */) {
          var sock = stream.node.sock;
          var msg = sock.sock_ops.recvmsg(sock, length);
          if (!msg) {
            // socket is closed
            return 0;
          }
          buffer.set(msg.buffer, offset);
          return msg.buffer.length;
        },write:function(stream, buffer, offset, length, position /* ignored */) {
          var sock = stream.node.sock;
          return sock.sock_ops.sendmsg(sock, buffer, offset, length);
        },close:function(stream) {
          var sock = stream.node.sock;
          sock.sock_ops.close(sock);
        }},nextname:function() {
        if (!SOCKFS.nextname.current) {
          SOCKFS.nextname.current = 0;
        }
        return 'socket[' + (SOCKFS.nextname.current++) + ']';
      },websocket_sock_ops:{createPeer:function(sock, addr, port) {
          var ws;
  
          if (typeof addr == 'object') {
            ws = addr;
            addr = null;
            port = null;
          }
  
          if (ws) {
            // for sockets that've already connected (e.g. we're the server)
            // we can inspect the _socket property for the address
            if (ws._socket) {
              addr = ws._socket.remoteAddress;
              port = ws._socket.remotePort;
            }
            // if we're just now initializing a connection to the remote,
            // inspect the url property
            else {
              var result = /ws[s]?:\/\/([^:]+):(\d+)/.exec(ws.url);
              if (!result) {
                throw new Error('WebSocket URL must be in the format ws(s)://address:port');
              }
              addr = result[1];
              port = parseInt(result[2], 10);
            }
          } else {
            // create the actual websocket object and connect
            try {
              // runtimeConfig gets set to true if WebSocket runtime configuration is available.
              var runtimeConfig = (Module['websocket'] && ('object' === typeof Module['websocket']));
  
              // The default value is 'ws://' the replace is needed because the compiler replaces '//' comments with '#'
              // comments without checking context, so we'd end up with ws:#, the replace swaps the '#' for '//' again.
              var url = 'ws:#'.replace('#', '//');
  
              if (runtimeConfig) {
                if ('string' === typeof Module['websocket']['url']) {
                  url = Module['websocket']['url']; // Fetch runtime WebSocket URL config.
                }
              }
  
              if (url === 'ws://' || url === 'wss://') { // Is the supplied URL config just a prefix, if so complete it.
                var parts = addr.split('/');
                url = url + parts[0] + ":" + port + "/" + parts.slice(1).join('/');
              }
  
              // Make the WebSocket subprotocol (Sec-WebSocket-Protocol) default to binary if no configuration is set.
              var subProtocols = 'binary'; // The default value is 'binary'
  
              if (runtimeConfig) {
                if ('string' === typeof Module['websocket']['subprotocol']) {
                  subProtocols = Module['websocket']['subprotocol']; // Fetch runtime WebSocket subprotocol config.
                }
              }
  
              // The default WebSocket options
              var opts = undefined;
  
              if (subProtocols !== 'null') {
                // The regex trims the string (removes spaces at the beginning and end, then splits the string by
                // <any space>,<any space> into an Array. Whitespace removal is important for Websockify and ws.
                subProtocols = subProtocols.replace(/^ +| +$/g,"").split(/ *, */);
  
                opts = subProtocols;
              }
  
              // some webservers (azure) does not support subprotocol header
              if (runtimeConfig && null === Module['websocket']['subprotocol']) {
                subProtocols = 'null';
                opts = undefined;
              }
  
              // If node we use the ws library.
              var WebSocketConstructor;
              if (ENVIRONMENT_IS_NODE) {
                WebSocketConstructor = /** @type{(typeof WebSocket)} */(require('ws'));
              } else
              {
                WebSocketConstructor = WebSocket;
              }
              ws = new WebSocketConstructor(url, opts);
              ws.binaryType = 'arraybuffer';
            } catch (e) {
              throw new FS.ErrnoError(23);
            }
          }
  
          var peer = {
            addr: addr,
            port: port,
            socket: ws,
            dgram_send_queue: []
          };
  
          SOCKFS.websocket_sock_ops.addPeer(sock, peer);
          SOCKFS.websocket_sock_ops.handlePeerEvents(sock, peer);
  
          // if this is a bound dgram socket, send the port number first to allow
          // us to override the ephemeral port reported to us by remotePort on the
          // remote end.
          if (sock.type === 2 && typeof sock.sport != 'undefined') {
            peer.dgram_send_queue.push(new Uint8Array([
                255, 255, 255, 255,
                'p'.charCodeAt(0), 'o'.charCodeAt(0), 'r'.charCodeAt(0), 't'.charCodeAt(0),
                ((sock.sport & 0xff00) >> 8) , (sock.sport & 0xff)
            ]));
          }
  
          return peer;
        },getPeer:function(sock, addr, port) {
          return sock.peers[addr + ':' + port];
        },addPeer:function(sock, peer) {
          sock.peers[peer.addr + ':' + peer.port] = peer;
        },removePeer:function(sock, peer) {
          delete sock.peers[peer.addr + ':' + peer.port];
        },handlePeerEvents:function(sock, peer) {
          var first = true;
  
          var handleOpen = function () {
  
            Module['websocket'].emit('open', sock.stream.fd);
  
            try {
              var queued = peer.dgram_send_queue.shift();
              while (queued) {
                peer.socket.send(queued);
                queued = peer.dgram_send_queue.shift();
              }
            } catch (e) {
              // not much we can do here in the way of proper error handling as we've already
              // lied and said this data was sent. shut it down.
              peer.socket.close();
            }
          };
  
          function handleMessage(data) {
            if (typeof data == 'string') {
              var encoder = new TextEncoder(); // should be utf-8
              data = encoder.encode(data); // make a typed array from the string
            } else {
              assert(data.byteLength !== undefined); // must receive an ArrayBuffer
              if (data.byteLength == 0) {
                // An empty ArrayBuffer will emit a pseudo disconnect event
                // as recv/recvmsg will return zero which indicates that a socket
                // has performed a shutdown although the connection has not been disconnected yet.
                return;
              } else {
                data = new Uint8Array(data); // make a typed array view on the array buffer
              }
            }
  
            // if this is the port message, override the peer's port with it
            var wasfirst = first;
            first = false;
            if (wasfirst &&
                data.length === 10 &&
                data[0] === 255 && data[1] === 255 && data[2] === 255 && data[3] === 255 &&
                data[4] === 'p'.charCodeAt(0) && data[5] === 'o'.charCodeAt(0) && data[6] === 'r'.charCodeAt(0) && data[7] === 't'.charCodeAt(0)) {
              // update the peer's port and it's key in the peer map
              var newport = ((data[8] << 8) | data[9]);
              SOCKFS.websocket_sock_ops.removePeer(sock, peer);
              peer.port = newport;
              SOCKFS.websocket_sock_ops.addPeer(sock, peer);
              return;
            }
  
            sock.recv_queue.push({ addr: peer.addr, port: peer.port, data: data });
            Module['websocket'].emit('message', sock.stream.fd);
          };
  
          if (ENVIRONMENT_IS_NODE) {
            peer.socket.on('open', handleOpen);
            peer.socket.on('message', function(data, isBinary) {
              if (!isBinary) {
                return;
              }
              handleMessage((new Uint8Array(data)).buffer); // copy from node Buffer -> ArrayBuffer
            });
            peer.socket.on('close', function() {
              Module['websocket'].emit('close', sock.stream.fd);
            });
            peer.socket.on('error', function(error) {
              // Although the ws library may pass errors that may be more descriptive than
              // ECONNREFUSED they are not necessarily the expected error code e.g. 
              // ENOTFOUND on getaddrinfo seems to be node.js specific, so using ECONNREFUSED
              // is still probably the most useful thing to do.
              sock.error = 14; // Used in getsockopt for SOL_SOCKET/SO_ERROR test.
              Module['websocket'].emit('error', [sock.stream.fd, sock.error, 'ECONNREFUSED: Connection refused']);
              // don't throw
            });
          } else {
            peer.socket.onopen = handleOpen;
            peer.socket.onclose = function() {
              Module['websocket'].emit('close', sock.stream.fd);
            };
            peer.socket.onmessage = function peer_socket_onmessage(event) {
              handleMessage(event.data);
            };
            peer.socket.onerror = function(error) {
              // The WebSocket spec only allows a 'simple event' to be thrown on error,
              // so we only really know as much as ECONNREFUSED.
              sock.error = 14; // Used in getsockopt for SOL_SOCKET/SO_ERROR test.
              Module['websocket'].emit('error', [sock.stream.fd, sock.error, 'ECONNREFUSED: Connection refused']);
            };
          }
        },poll:function(sock) {
          if (sock.type === 1 && sock.server) {
            // listen sockets should only say they're available for reading
            // if there are pending clients.
            return sock.pending.length ? (64 | 1) : 0;
          }
  
          var mask = 0;
          var dest = sock.type === 1 ?  // we only care about the socket state for connection-based sockets
            SOCKFS.websocket_sock_ops.getPeer(sock, sock.daddr, sock.dport) :
            null;
  
          if (sock.recv_queue.length ||
              !dest ||  // connection-less sockets are always ready to read
              (dest && dest.socket.readyState === dest.socket.CLOSING) ||
              (dest && dest.socket.readyState === dest.socket.CLOSED)) {  // let recv return 0 once closed
            mask |= (64 | 1);
          }
  
          if (!dest ||  // connection-less sockets are always ready to write
              (dest && dest.socket.readyState === dest.socket.OPEN)) {
            mask |= 4;
          }
  
          if ((dest && dest.socket.readyState === dest.socket.CLOSING) ||
              (dest && dest.socket.readyState === dest.socket.CLOSED)) {
            mask |= 16;
          }
  
          return mask;
        },ioctl:function(sock, request, arg) {
          switch (request) {
            case 21531:
              var bytes = 0;
              if (sock.recv_queue.length) {
                bytes = sock.recv_queue[0].data.length;
              }
              HEAP32[((arg)>>2)] = bytes;
              return 0;
            default:
              return 28;
          }
        },close:function(sock) {
          // if we've spawned a listen server, close it
          if (sock.server) {
            try {
              sock.server.close();
            } catch (e) {
            }
            sock.server = null;
          }
          // close any peer connections
          var peers = Object.keys(sock.peers);
          for (var i = 0; i < peers.length; i++) {
            var peer = sock.peers[peers[i]];
            try {
              peer.socket.close();
            } catch (e) {
            }
            SOCKFS.websocket_sock_ops.removePeer(sock, peer);
          }
          return 0;
        },bind:function(sock, addr, port) {
          if (typeof sock.saddr != 'undefined' || typeof sock.sport != 'undefined') {
            throw new FS.ErrnoError(28);  // already bound
          }
          sock.saddr = addr;
          sock.sport = port;
          // in order to emulate dgram sockets, we need to launch a listen server when
          // binding on a connection-less socket
          // note: this is only required on the server side
          if (sock.type === 2) {
            // close the existing server if it exists
            if (sock.server) {
              sock.server.close();
              sock.server = null;
            }
            // swallow error operation not supported error that occurs when binding in the
            // browser where this isn't supported
            try {
              sock.sock_ops.listen(sock, 0);
            } catch (e) {
              if (!(e instanceof FS.ErrnoError)) throw e;
              if (e.errno !== 138) throw e;
            }
          }
        },connect:function(sock, addr, port) {
          if (sock.server) {
            throw new FS.ErrnoError(138);
          }
  
          // TODO autobind
          // if (!sock.addr && sock.type == 2) {
          // }
  
          // early out if we're already connected / in the middle of connecting
          if (typeof sock.daddr != 'undefined' && typeof sock.dport != 'undefined') {
            var dest = SOCKFS.websocket_sock_ops.getPeer(sock, sock.daddr, sock.dport);
            if (dest) {
              if (dest.socket.readyState === dest.socket.CONNECTING) {
                throw new FS.ErrnoError(7);
              } else {
                throw new FS.ErrnoError(30);
              }
            }
          }
  
          // add the socket to our peer list and set our
          // destination address / port to match
          var peer = SOCKFS.websocket_sock_ops.createPeer(sock, addr, port);
          sock.daddr = peer.addr;
          sock.dport = peer.port;
  
          // always "fail" in non-blocking mode
          throw new FS.ErrnoError(26);
        },listen:function(sock, backlog) {
          if (!ENVIRONMENT_IS_NODE) {
            throw new FS.ErrnoError(138);
          }
          if (sock.server) {
             throw new FS.ErrnoError(28);  // already listening
          }
          var WebSocketServer = require('ws').Server;
          var host = sock.saddr;
          sock.server = new WebSocketServer({
            host: host,
            port: sock.sport
            // TODO support backlog
          });
          Module['websocket'].emit('listen', sock.stream.fd); // Send Event with listen fd.
  
          sock.server.on('connection', function(ws) {
            if (sock.type === 1) {
              var newsock = SOCKFS.createSocket(sock.family, sock.type, sock.protocol);
  
              // create a peer on the new socket
              var peer = SOCKFS.websocket_sock_ops.createPeer(newsock, ws);
              newsock.daddr = peer.addr;
              newsock.dport = peer.port;
  
              // push to queue for accept to pick up
              sock.pending.push(newsock);
              Module['websocket'].emit('connection', newsock.stream.fd);
            } else {
              // create a peer on the listen socket so calling sendto
              // with the listen socket and an address will resolve
              // to the correct client
              SOCKFS.websocket_sock_ops.createPeer(sock, ws);
              Module['websocket'].emit('connection', sock.stream.fd);
            }
          });
          sock.server.on('close', function() {
            Module['websocket'].emit('close', sock.stream.fd);
            sock.server = null;
          });
          sock.server.on('error', function(error) {
            // Although the ws library may pass errors that may be more descriptive than
            // ECONNREFUSED they are not necessarily the expected error code e.g. 
            // ENOTFOUND on getaddrinfo seems to be node.js specific, so using EHOSTUNREACH
            // is still probably the most useful thing to do. This error shouldn't
            // occur in a well written app as errors should get trapped in the compiled
            // app's own getaddrinfo call.
            sock.error = 23; // Used in getsockopt for SOL_SOCKET/SO_ERROR test.
            Module['websocket'].emit('error', [sock.stream.fd, sock.error, 'EHOSTUNREACH: Host is unreachable']);
            // don't throw
          });
        },accept:function(listensock) {
          if (!listensock.server || !listensock.pending.length) {
            throw new FS.ErrnoError(28);
          }
          var newsock = listensock.pending.shift();
          newsock.stream.flags = listensock.stream.flags;
          return newsock;
        },getname:function(sock, peer) {
          var addr, port;
          if (peer) {
            if (sock.daddr === undefined || sock.dport === undefined) {
              throw new FS.ErrnoError(53);
            }
            addr = sock.daddr;
            port = sock.dport;
          } else {
            // TODO saddr and sport will be set for bind()'d UDP sockets, but what
            // should we be returning for TCP sockets that've been connect()'d?
            addr = sock.saddr || 0;
            port = sock.sport || 0;
          }
          return { addr: addr, port: port };
        },sendmsg:function(sock, buffer, offset, length, addr, port) {
          if (sock.type === 2) {
            // connection-less sockets will honor the message address,
            // and otherwise fall back to the bound destination address
            if (addr === undefined || port === undefined) {
              addr = sock.daddr;
              port = sock.dport;
            }
            // if there was no address to fall back to, error out
            if (addr === undefined || port === undefined) {
              throw new FS.ErrnoError(17);
            }
          } else {
            // connection-based sockets will only use the bound
            addr = sock.daddr;
            port = sock.dport;
          }
  
          // find the peer for the destination address
          var dest = SOCKFS.websocket_sock_ops.getPeer(sock, addr, port);
  
          // early out if not connected with a connection-based socket
          if (sock.type === 1) {
            if (!dest || dest.socket.readyState === dest.socket.CLOSING || dest.socket.readyState === dest.socket.CLOSED) {
              throw new FS.ErrnoError(53);
            } else if (dest.socket.readyState === dest.socket.CONNECTING) {
              throw new FS.ErrnoError(6);
            }
          }
  
          // create a copy of the incoming data to send, as the WebSocket API
          // doesn't work entirely with an ArrayBufferView, it'll just send
          // the entire underlying buffer
          if (ArrayBuffer.isView(buffer)) {
            offset += buffer.byteOffset;
            buffer = buffer.buffer;
          }
  
          var data;
            data = buffer.slice(offset, offset + length);
  
          // if we're emulating a connection-less dgram socket and don't have
          // a cached connection, queue the buffer to send upon connect and
          // lie, saying the data was sent now.
          if (sock.type === 2) {
            if (!dest || dest.socket.readyState !== dest.socket.OPEN) {
              // if we're not connected, open a new connection
              if (!dest || dest.socket.readyState === dest.socket.CLOSING || dest.socket.readyState === dest.socket.CLOSED) {
                dest = SOCKFS.websocket_sock_ops.createPeer(sock, addr, port);
              }
              dest.dgram_send_queue.push(data);
              return length;
            }
          }
  
          try {
            // send the actual data
            dest.socket.send(data);
            return length;
          } catch (e) {
            throw new FS.ErrnoError(28);
          }
        },recvmsg:function(sock, length) {
          // http://pubs.opengroup.org/onlinepubs/7908799/xns/recvmsg.html
          if (sock.type === 1 && sock.server) {
            // tcp servers should not be recv()'ing on the listen socket
            throw new FS.ErrnoError(53);
          }
  
          var queued = sock.recv_queue.shift();
          if (!queued) {
            if (sock.type === 1) {
              var dest = SOCKFS.websocket_sock_ops.getPeer(sock, sock.daddr, sock.dport);
  
              if (!dest) {
                // if we have a destination address but are not connected, error out
                throw new FS.ErrnoError(53);
              }
              else if (dest.socket.readyState === dest.socket.CLOSING || dest.socket.readyState === dest.socket.CLOSED) {
                // return null if the socket has closed
                return null;
              }
              else {
                // else, our socket is in a valid state but truly has nothing available
                throw new FS.ErrnoError(6);
              }
            } else {
              throw new FS.ErrnoError(6);
            }
          }
  
          // queued.data will be an ArrayBuffer if it's unadulterated, but if it's
          // requeued TCP data it'll be an ArrayBufferView
          var queuedLength = queued.data.byteLength || queued.data.length;
          var queuedOffset = queued.data.byteOffset || 0;
          var queuedBuffer = queued.data.buffer || queued.data;
          var bytesRead = Math.min(length, queuedLength);
          var res = {
            buffer: new Uint8Array(queuedBuffer, queuedOffset, bytesRead),
            addr: queued.addr,
            port: queued.port
          };
  
          // push back any unread data for TCP connections
          if (sock.type === 1 && bytesRead < queuedLength) {
            var bytesRemaining = queuedLength - bytesRead;
            queued.data = new Uint8Array(queuedBuffer, queuedOffset + bytesRead, bytesRemaining);
            sock.recv_queue.unshift(queued);
          }
  
          return res;
        }}};
  function getSocketFromFD(fd) {
      var socket = SOCKFS.getSocket(fd);
      if (!socket) throw new FS.ErrnoError(8);
      return socket;
    }
  
  function setErrNo(value) {
      HEAP32[((___errno_location())>>2)] = value;
      return value;
    }
  var Sockets = {BUFFER_SIZE:10240,MAX_BUFFER_SIZE:10485760,nextFd:1,fds:{},nextport:1,maxport:65535,peer:null,connections:{},portmap:{},localAddr:4261412874,addrPool:[33554442,50331658,67108874,83886090,100663306,117440522,134217738,150994954,167772170,184549386,201326602,218103818,234881034]};
  
  function inetNtop4(addr) {
      return (addr & 0xff) + '.' + ((addr >> 8) & 0xff) + '.' + ((addr >> 16) & 0xff) + '.' + ((addr >> 24) & 0xff)
    }
  
  function inetNtop6(ints) {
      //  ref:  http://www.ietf.org/rfc/rfc2373.txt - section 2.5.4
      //  Format for IPv4 compatible and mapped  128-bit IPv6 Addresses
      //  128-bits are split into eight 16-bit words
      //  stored in network byte order (big-endian)
      //  |                80 bits               | 16 |      32 bits        |
      //  +-----------------------------------------------------------------+
      //  |               10 bytes               |  2 |      4 bytes        |
      //  +--------------------------------------+--------------------------+
      //  +               5 words                |  1 |      2 words        |
      //  +--------------------------------------+--------------------------+
      //  |0000..............................0000|0000|    IPv4 ADDRESS     | (compatible)
      //  +--------------------------------------+----+---------------------+
      //  |0000..............................0000|FFFF|    IPv4 ADDRESS     | (mapped)
      //  +--------------------------------------+----+---------------------+
      var str = "";
      var word = 0;
      var longest = 0;
      var lastzero = 0;
      var zstart = 0;
      var len = 0;
      var i = 0;
      var parts = [
        ints[0] & 0xffff,
        (ints[0] >> 16),
        ints[1] & 0xffff,
        (ints[1] >> 16),
        ints[2] & 0xffff,
        (ints[2] >> 16),
        ints[3] & 0xffff,
        (ints[3] >> 16)
      ];
  
      // Handle IPv4-compatible, IPv4-mapped, loopback and any/unspecified addresses
  
      var hasipv4 = true;
      var v4part = "";
      // check if the 10 high-order bytes are all zeros (first 5 words)
      for (i = 0; i < 5; i++) {
        if (parts[i] !== 0) { hasipv4 = false; break; }
      }
  
      if (hasipv4) {
        // low-order 32-bits store an IPv4 address (bytes 13 to 16) (last 2 words)
        v4part = inetNtop4(parts[6] | (parts[7] << 16));
        // IPv4-mapped IPv6 address if 16-bit value (bytes 11 and 12) == 0xFFFF (6th word)
        if (parts[5] === -1) {
          str = "::ffff:";
          str += v4part;
          return str;
        }
        // IPv4-compatible IPv6 address if 16-bit value (bytes 11 and 12) == 0x0000 (6th word)
        if (parts[5] === 0) {
          str = "::";
          //special case IPv6 addresses
          if (v4part === "0.0.0.0") v4part = ""; // any/unspecified address
          if (v4part === "0.0.0.1") v4part = "1";// loopback address
          str += v4part;
          return str;
        }
      }
  
      // Handle all other IPv6 addresses
  
      // first run to find the longest contiguous zero words
      for (word = 0; word < 8; word++) {
        if (parts[word] === 0) {
          if (word - lastzero > 1) {
            len = 0;
          }
          lastzero = word;
          len++;
        }
        if (len > longest) {
          longest = len;
          zstart = word - longest + 1;
        }
      }
  
      for (word = 0; word < 8; word++) {
        if (longest > 1) {
          // compress contiguous zeros - to produce "::"
          if (parts[word] === 0 && word >= zstart && word < (zstart + longest) ) {
            if (word === zstart) {
              str += ":";
              if (zstart === 0) str += ":"; //leading zeros case
            }
            continue;
          }
        }
        // converts 16-bit words from big-endian to little-endian before converting to hex string
        str += Number(_ntohs(parts[word] & 0xffff)).toString(16);
        str += word < 7 ? ":" : "";
      }
      return str;
    }
  function readSockaddr(sa, salen) {
      // family / port offsets are common to both sockaddr_in and sockaddr_in6
      var family = HEAP16[((sa)>>1)];
      var port = _ntohs(HEAPU16[(((sa)+(2))>>1)]);
      var addr;
  
      switch (family) {
        case 2:
          if (salen !== 16) {
            return { errno: 28 };
          }
          addr = HEAP32[(((sa)+(4))>>2)];
          addr = inetNtop4(addr);
          break;
        case 10:
          if (salen !== 28) {
            return { errno: 28 };
          }
          addr = [
            HEAP32[(((sa)+(8))>>2)],
            HEAP32[(((sa)+(12))>>2)],
            HEAP32[(((sa)+(16))>>2)],
            HEAP32[(((sa)+(20))>>2)]
          ];
          addr = inetNtop6(addr);
          break;
        default:
          return { errno: 5 };
      }
  
      return { family: family, addr: addr, port: port };
    }
  
  function inetPton4(str) {
      var b = str.split('.');
      for (var i = 0; i < 4; i++) {
        var tmp = Number(b[i]);
        if (isNaN(tmp)) return null;
        b[i] = tmp;
      }
      return (b[0] | (b[1] << 8) | (b[2] << 16) | (b[3] << 24)) >>> 0;
    }
  
  /** @suppress {checkTypes} */
  function jstoi_q(str) {
      return parseInt(str);
    }
  function inetPton6(str) {
      var words;
      var w, offset, z, i;
      /* http://home.deds.nl/~aeron/regex/ */
      var valid6regx = /^((?=.*::)(?!.*::.+::)(::)?([\dA-F]{1,4}:(:|\b)|){5}|([\dA-F]{1,4}:){6})((([\dA-F]{1,4}((?!\3)::|:\b|$))|(?!\2\3)){2}|(((2[0-4]|1\d|[1-9])?\d|25[0-5])\.?\b){4})$/i
      var parts = [];
      if (!valid6regx.test(str)) {
        return null;
      }
      if (str === "::") {
        return [0, 0, 0, 0, 0, 0, 0, 0];
      }
      // Z placeholder to keep track of zeros when splitting the string on ":"
      if (str.startsWith("::")) {
        str = str.replace("::", "Z:"); // leading zeros case
      } else {
        str = str.replace("::", ":Z:");
      }
  
      if (str.indexOf(".") > 0) {
        // parse IPv4 embedded stress
        str = str.replace(new RegExp('[.]', 'g'), ":");
        words = str.split(":");
        words[words.length-4] = jstoi_q(words[words.length-4]) + jstoi_q(words[words.length-3])*256;
        words[words.length-3] = jstoi_q(words[words.length-2]) + jstoi_q(words[words.length-1])*256;
        words = words.slice(0, words.length-2);
      } else {
        words = str.split(":");
      }
  
      offset = 0; z = 0;
      for (w=0; w < words.length; w++) {
        if (typeof words[w] == 'string') {
          if (words[w] === 'Z') {
            // compressed zeros - write appropriate number of zero words
            for (z = 0; z < (8 - words.length+1); z++) {
              parts[w+z] = 0;
            }
            offset = z-1;
          } else {
            // parse hex to field to 16-bit value and write it in network byte-order
            parts[w+offset] = _htons(parseInt(words[w],16));
          }
        } else {
          // parsed IPv4 words
          parts[w+offset] = words[w];
        }
      }
      return [
        (parts[1] << 16) | parts[0],
        (parts[3] << 16) | parts[2],
        (parts[5] << 16) | parts[4],
        (parts[7] << 16) | parts[6]
      ];
    }
  var DNS = {address_map:{id:1,addrs:{},names:{}},lookup_name:function (name) {
        // If the name is already a valid ipv4 / ipv6 address, don't generate a fake one.
        var res = inetPton4(name);
        if (res !== null) {
          return name;
        }
        res = inetPton6(name);
        if (res !== null) {
          return name;
        }
  
        // See if this name is already mapped.
        var addr;
  
        if (DNS.address_map.addrs[name]) {
          addr = DNS.address_map.addrs[name];
        } else {
          var id = DNS.address_map.id++;
          assert(id < 65535, 'exceeded max address mappings of 65535');
  
          addr = '172.29.' + (id & 0xff) + '.' + (id & 0xff00);
  
          DNS.address_map.names[addr] = name;
          DNS.address_map.addrs[name] = addr;
        }
  
        return addr;
      },lookup_addr:function (addr) {
        if (DNS.address_map.names[addr]) {
          return DNS.address_map.names[addr];
        }
  
        return null;
      }};
  /** @param {boolean=} allowNull */
  function getSocketAddress(addrp, addrlen, allowNull) {
      if (allowNull && addrp === 0) return null;
      var info = readSockaddr(addrp, addrlen);
      if (info.errno) throw new FS.ErrnoError(info.errno);
      info.addr = DNS.lookup_addr(info.addr) || info.addr;
      return info;
    }
  
  var SYSCALLS = {DEFAULT_POLLMASK:5,calculateAt:function(dirfd, path, allowEmpty) {
        if (PATH.isAbs(path)) {
          return path;
        }
        // relative path
        var dir;
        if (dirfd === -100) {
          dir = FS.cwd();
        } else {
          var dirstream = FS.getStream(dirfd);
          if (!dirstream) throw new FS.ErrnoError(8);
          dir = dirstream.path;
        }
        if (path.length == 0) {
          if (!allowEmpty) {
            throw new FS.ErrnoError(44);;
          }
          return dir;
        }
        return PATH.join2(dir, path);
      },doStat:function(func, path, buf) {
        try {
          var stat = func(path);
        } catch (e) {
          if (e && e.node && PATH.normalize(path) !== PATH.normalize(FS.getPath(e.node))) {
            // an error occurred while trying to look up the path; we should just report ENOTDIR
            return -54;
          }
          throw e;
        }
        HEAP32[((buf)>>2)] = stat.dev;
        HEAP32[(((buf)+(4))>>2)] = 0;
        HEAP32[(((buf)+(8))>>2)] = stat.ino;
        HEAP32[(((buf)+(12))>>2)] = stat.mode;
        HEAP32[(((buf)+(16))>>2)] = stat.nlink;
        HEAP32[(((buf)+(20))>>2)] = stat.uid;
        HEAP32[(((buf)+(24))>>2)] = stat.gid;
        HEAP32[(((buf)+(28))>>2)] = stat.rdev;
        HEAP32[(((buf)+(32))>>2)] = 0;
        (tempI64 = [stat.size>>>0,(tempDouble=stat.size,(+(Math.abs(tempDouble))) >= 1.0 ? (tempDouble > 0.0 ? ((Math.min((+(Math.floor((tempDouble)/4294967296.0))), 4294967295.0))|0)>>>0 : (~~((+(Math.ceil((tempDouble - +(((~~(tempDouble)))>>>0))/4294967296.0)))))>>>0) : 0)],HEAP32[(((buf)+(40))>>2)] = tempI64[0],HEAP32[(((buf)+(44))>>2)] = tempI64[1]);
        HEAP32[(((buf)+(48))>>2)] = 4096;
        HEAP32[(((buf)+(52))>>2)] = stat.blocks;
        HEAP32[(((buf)+(56))>>2)] = (stat.atime.getTime() / 1000)|0;
        HEAP32[(((buf)+(60))>>2)] = 0;
        HEAP32[(((buf)+(64))>>2)] = (stat.mtime.getTime() / 1000)|0;
        HEAP32[(((buf)+(68))>>2)] = 0;
        HEAP32[(((buf)+(72))>>2)] = (stat.ctime.getTime() / 1000)|0;
        HEAP32[(((buf)+(76))>>2)] = 0;
        (tempI64 = [stat.ino>>>0,(tempDouble=stat.ino,(+(Math.abs(tempDouble))) >= 1.0 ? (tempDouble > 0.0 ? ((Math.min((+(Math.floor((tempDouble)/4294967296.0))), 4294967295.0))|0)>>>0 : (~~((+(Math.ceil((tempDouble - +(((~~(tempDouble)))>>>0))/4294967296.0)))))>>>0) : 0)],HEAP32[(((buf)+(80))>>2)] = tempI64[0],HEAP32[(((buf)+(84))>>2)] = tempI64[1]);
        return 0;
      },doMsync:function(addr, stream, len, flags, offset) {
        var buffer = HEAPU8.slice(addr, addr + len);
        FS.msync(stream, buffer, offset, len, flags);
      },varargs:undefined,get:function() {
        SYSCALLS.varargs += 4;
        var ret = HEAP32[(((SYSCALLS.varargs)-(4))>>2)];
        return ret;
      },getStr:function(ptr) {
        var ret = UTF8ToString(ptr);
        return ret;
      },getStreamFromFD:function(fd) {
        var stream = FS.getStream(fd);
        if (!stream) throw new FS.ErrnoError(8);
        return stream;
      }};
  function ___syscall_connect(fd, addr, addrlen) {
  try {
  
      var sock = getSocketFromFD(fd);
      var info = getSocketAddress(addr, addrlen);
      sock.sock_ops.connect(sock, info.addr, info.port);
      return 0;
    } catch (e) {
    if (typeof FS == 'undefined' || !(e instanceof FS.ErrnoError)) throw e;
    return -e.errno;
  }
  }

  function ___syscall_faccessat(dirfd, path, amode, flags) {
  try {
  
      path = SYSCALLS.getStr(path);
      path = SYSCALLS.calculateAt(dirfd, path);
      if (amode & ~7) {
        // need a valid mode
        return -28;
      }
      var lookup = FS.lookupPath(path, { follow: true });
      var node = lookup.node;
      if (!node) {
        return -44;
      }
      var perms = '';
      if (amode & 4) perms += 'r';
      if (amode & 2) perms += 'w';
      if (amode & 1) perms += 'x';
      if (perms /* otherwise, they've just passed F_OK */ && FS.nodePermissions(node, perms)) {
        return -2;
      }
      return 0;
    } catch (e) {
    if (typeof FS == 'undefined' || !(e instanceof FS.ErrnoError)) throw e;
    return -e.errno;
  }
  }

  function ___syscall_fadvise64(fd, offset, len, advice) {
      return 0; // your advice is important to us (but we can't use it)
    }

  function ___syscall_fcntl64(fd, cmd, varargs) {
  SYSCALLS.varargs = varargs;
  try {
  
      var stream = SYSCALLS.getStreamFromFD(fd);
      switch (cmd) {
        case 0: {
          var arg = SYSCALLS.get();
          if (arg < 0) {
            return -28;
          }
          var newStream;
          newStream = FS.createStream(stream, arg);
          return newStream.fd;
        }
        case 1:
        case 2:
          return 0;  // FD_CLOEXEC makes no sense for a single process.
        case 3:
          return stream.flags;
        case 4: {
          var arg = SYSCALLS.get();
          stream.flags |= arg;
          return 0;
        }
        case 5:
        /* case 5: Currently in musl F_GETLK64 has same value as F_GETLK, so omitted to avoid duplicate case blocks. If that changes, uncomment this */ {
          
          var arg = SYSCALLS.get();
          var offset = 0;
          // We're always unlocked.
          HEAP16[(((arg)+(offset))>>1)] = 2;
          return 0;
        }
        case 6:
        case 7:
        /* case 6: Currently in musl F_SETLK64 has same value as F_SETLK, so omitted to avoid duplicate case blocks. If that changes, uncomment this */
        /* case 7: Currently in musl F_SETLKW64 has same value as F_SETLKW, so omitted to avoid duplicate case blocks. If that changes, uncomment this */
          
          
          return 0; // Pretend that the locking is successful.
        case 16:
        case 8:
          return -28; // These are for sockets. We don't have them fully implemented yet.
        case 9:
          // musl trusts getown return values, due to a bug where they must be, as they overlap with errors. just return -1 here, so fcntl() returns that, and we set errno ourselves.
          setErrNo(28);
          return -1;
        default: {
          return -28;
        }
      }
    } catch (e) {
    if (typeof FS == 'undefined' || !(e instanceof FS.ErrnoError)) throw e;
    return -e.errno;
  }
  }

  function ___syscall_fstat64(fd, buf) {
  try {
  
      var stream = SYSCALLS.getStreamFromFD(fd);
      return SYSCALLS.doStat(FS.stat, stream.path, buf);
    } catch (e) {
    if (typeof FS == 'undefined' || !(e instanceof FS.ErrnoError)) throw e;
    return -e.errno;
  }
  }

  function ___syscall_statfs64(path, size, buf) {
  try {
  
      path = SYSCALLS.getStr(path);
      // NOTE: None of the constants here are true. We're just returning safe and
      //       sane values.
      HEAP32[(((buf)+(4))>>2)] = 4096;
      HEAP32[(((buf)+(40))>>2)] = 4096;
      HEAP32[(((buf)+(8))>>2)] = 1000000;
      HEAP32[(((buf)+(12))>>2)] = 500000;
      HEAP32[(((buf)+(16))>>2)] = 500000;
      HEAP32[(((buf)+(20))>>2)] = FS.nextInode;
      HEAP32[(((buf)+(24))>>2)] = 1000000;
      HEAP32[(((buf)+(28))>>2)] = 42;
      HEAP32[(((buf)+(44))>>2)] = 2;  // ST_NOSUID
      HEAP32[(((buf)+(36))>>2)] = 255;
      return 0;
    } catch (e) {
    if (typeof FS == 'undefined' || !(e instanceof FS.ErrnoError)) throw e;
    return -e.errno;
  }
  }
  function ___syscall_fstatfs64(fd, size, buf) {
  try {
  
      var stream = SYSCALLS.getStreamFromFD(fd);
      return ___syscall_statfs64(0, size, buf);
    } catch (e) {
    if (typeof FS == 'undefined' || !(e instanceof FS.ErrnoError)) throw e;
    return -e.errno;
  }
  }

  function convertI32PairToI53Checked(lo, hi) {
      return ((hi + 0x200000) >>> 0 < 0x400001 - !!lo) ? (lo >>> 0) + hi * 4294967296 : NaN;
    }
  function ___syscall_ftruncate64(fd, length_low, length_high) {
  try {
  
      var length = convertI32PairToI53Checked(length_low, length_high); if (isNaN(length)) return -61;
      FS.ftruncate(fd, length);
      return 0;
    } catch (e) {
    if (typeof FS == 'undefined' || !(e instanceof FS.ErrnoError)) throw e;
    return -e.errno;
  }
  }

  function ___syscall_getcwd(buf, size) {
  try {
  
      if (size === 0) return -28;
      var cwd = FS.cwd();
      var cwdLengthInBytes = lengthBytesUTF8(cwd) + 1;
      if (size < cwdLengthInBytes) return -68;
      stringToUTF8(cwd, buf, size);
      return cwdLengthInBytes;
    } catch (e) {
    if (typeof FS == 'undefined' || !(e instanceof FS.ErrnoError)) throw e;
    return -e.errno;
  }
  }

  function ___syscall_getdents64(fd, dirp, count) {
  try {
  
      var stream = SYSCALLS.getStreamFromFD(fd)
      if (!stream.getdents) {
        stream.getdents = FS.readdir(stream.path);
      }
  
      var struct_size = 280;
      var pos = 0;
      var off = FS.llseek(stream, 0, 1);
  
      var idx = Math.floor(off / struct_size);
  
      while (idx < stream.getdents.length && pos + struct_size <= count) {
        var id;
        var type;
        var name = stream.getdents[idx];
        if (name === '.') {
          id = stream.node.id;
          type = 4; // DT_DIR
        }
        else if (name === '..') {
          var lookup = FS.lookupPath(stream.path, { parent: true });
          id = lookup.node.id;
          type = 4; // DT_DIR
        }
        else {
          var child = FS.lookupNode(stream.node, name);
          id = child.id;
          type = FS.isChrdev(child.mode) ? 2 :  // DT_CHR, character device.
                 FS.isDir(child.mode) ? 4 :     // DT_DIR, directory.
                 FS.isLink(child.mode) ? 10 :   // DT_LNK, symbolic link.
                 8;                             // DT_REG, regular file.
        }
        (tempI64 = [id>>>0,(tempDouble=id,(+(Math.abs(tempDouble))) >= 1.0 ? (tempDouble > 0.0 ? ((Math.min((+(Math.floor((tempDouble)/4294967296.0))), 4294967295.0))|0)>>>0 : (~~((+(Math.ceil((tempDouble - +(((~~(tempDouble)))>>>0))/4294967296.0)))))>>>0) : 0)],HEAP32[((dirp + pos)>>2)] = tempI64[0],HEAP32[(((dirp + pos)+(4))>>2)] = tempI64[1]);
        (tempI64 = [(idx + 1) * struct_size>>>0,(tempDouble=(idx + 1) * struct_size,(+(Math.abs(tempDouble))) >= 1.0 ? (tempDouble > 0.0 ? ((Math.min((+(Math.floor((tempDouble)/4294967296.0))), 4294967295.0))|0)>>>0 : (~~((+(Math.ceil((tempDouble - +(((~~(tempDouble)))>>>0))/4294967296.0)))))>>>0) : 0)],HEAP32[(((dirp + pos)+(8))>>2)] = tempI64[0],HEAP32[(((dirp + pos)+(12))>>2)] = tempI64[1]);
        HEAP16[(((dirp + pos)+(16))>>1)] = 280;
        HEAP8[(((dirp + pos)+(18))>>0)] = type;
        stringToUTF8(name, dirp + pos + 19, 256);
        pos += struct_size;
        idx += 1;
      }
      FS.llseek(stream, idx * struct_size, 0);
      return pos;
    } catch (e) {
    if (typeof FS == 'undefined' || !(e instanceof FS.ErrnoError)) throw e;
    return -e.errno;
  }
  }

  function ___syscall_ioctl(fd, op, varargs) {
  SYSCALLS.varargs = varargs;
  try {
  
      var stream = SYSCALLS.getStreamFromFD(fd);
      switch (op) {
        case 21509:
        case 21505: {
          if (!stream.tty) return -59;
          return 0;
        }
        case 21510:
        case 21511:
        case 21512:
        case 21506:
        case 21507:
        case 21508: {
          if (!stream.tty) return -59;
          return 0; // no-op, not actually adjusting terminal settings
        }
        case 21519: {
          if (!stream.tty) return -59;
          var argp = SYSCALLS.get();
          HEAP32[((argp)>>2)] = 0;
          return 0;
        }
        case 21520: {
          if (!stream.tty) return -59;
          return -28; // not supported
        }
        case 21531: {
          var argp = SYSCALLS.get();
          return FS.ioctl(stream, op, argp);
        }
        case 21523: {
          // TODO: in theory we should write to the winsize struct that gets
          // passed in, but for now musl doesn't read anything on it
          if (!stream.tty) return -59;
          return 0;
        }
        case 21524: {
          // TODO: technically, this ioctl call should change the window size.
          // but, since emscripten doesn't have any concept of a terminal window
          // yet, we'll just silently throw it away as we do TIOCGWINSZ
          if (!stream.tty) return -59;
          return 0;
        }
        default: abort('bad ioctl syscall ' + op);
      }
    } catch (e) {
    if (typeof FS == 'undefined' || !(e instanceof FS.ErrnoError)) throw e;
    return -e.errno;
  }
  }

  function ___syscall_lstat64(path, buf) {
  try {
  
      path = SYSCALLS.getStr(path);
      return SYSCALLS.doStat(FS.lstat, path, buf);
    } catch (e) {
    if (typeof FS == 'undefined' || !(e instanceof FS.ErrnoError)) throw e;
    return -e.errno;
  }
  }

  function ___syscall_newfstatat(dirfd, path, buf, flags) {
  try {
  
      path = SYSCALLS.getStr(path);
      var nofollow = flags & 256;
      var allowEmpty = flags & 4096;
      flags = flags & (~4352);
      path = SYSCALLS.calculateAt(dirfd, path, allowEmpty);
      return SYSCALLS.doStat(nofollow ? FS.lstat : FS.stat, path, buf);
    } catch (e) {
    if (typeof FS == 'undefined' || !(e instanceof FS.ErrnoError)) throw e;
    return -e.errno;
  }
  }

  function ___syscall_openat(dirfd, path, flags, varargs) {
  SYSCALLS.varargs = varargs;
  try {
  
      path = SYSCALLS.getStr(path);
      path = SYSCALLS.calculateAt(dirfd, path);
      var mode = varargs ? SYSCALLS.get() : 0;
      return FS.open(path, flags, mode).fd;
    } catch (e) {
    if (typeof FS == 'undefined' || !(e instanceof FS.ErrnoError)) throw e;
    return -e.errno;
  }
  }

  function ___syscall_readlinkat(dirfd, path, buf, bufsize) {
  try {
  
      path = SYSCALLS.getStr(path);
      path = SYSCALLS.calculateAt(dirfd, path);
      if (bufsize <= 0) return -28;
      var ret = FS.readlink(path);
  
      var len = Math.min(bufsize, lengthBytesUTF8(ret));
      var endChar = HEAP8[buf+len];
      stringToUTF8(ret, buf, bufsize+1);
      // readlink is one of the rare functions that write out a C string, but does never append a null to the output buffer(!)
      // stringToUTF8() always appends a null byte, so restore the character under the null byte after the write.
      HEAP8[buf+len] = endChar;
      return len;
    } catch (e) {
    if (typeof FS == 'undefined' || !(e instanceof FS.ErrnoError)) throw e;
    return -e.errno;
  }
  }

  /** @param {number=} addrlen */
  function writeSockaddr(sa, family, addr, port, addrlen) {
      switch (family) {
        case 2:
          addr = inetPton4(addr);
          zeroMemory(sa, 16);
          if (addrlen) {
            HEAP32[((addrlen)>>2)] = 16;
          }
          HEAP16[((sa)>>1)] = family;
          HEAP32[(((sa)+(4))>>2)] = addr;
          HEAP16[(((sa)+(2))>>1)] = _htons(port);
          break;
        case 10:
          addr = inetPton6(addr);
          zeroMemory(sa, 28);
          if (addrlen) {
            HEAP32[((addrlen)>>2)] = 28;
          }
          HEAP32[((sa)>>2)] = family;
          HEAP32[(((sa)+(8))>>2)] = addr[0];
          HEAP32[(((sa)+(12))>>2)] = addr[1];
          HEAP32[(((sa)+(16))>>2)] = addr[2];
          HEAP32[(((sa)+(20))>>2)] = addr[3];
          HEAP16[(((sa)+(2))>>1)] = _htons(port);
          break;
        default:
          return 5;
      }
      return 0;
    }
  function ___syscall_recvfrom(fd, buf, len, flags, addr, addrlen) {
  try {
  
      var sock = getSocketFromFD(fd);
      var msg = sock.sock_ops.recvmsg(sock, len);
      if (!msg) return 0; // socket is closed
      if (addr) {
        var errno = writeSockaddr(addr, sock.family, DNS.lookup_name(msg.addr), msg.port, addrlen);
      }
      HEAPU8.set(msg.buffer, buf);
      return msg.buffer.byteLength;
    } catch (e) {
    if (typeof FS == 'undefined' || !(e instanceof FS.ErrnoError)) throw e;
    return -e.errno;
  }
  }

  function ___syscall_sendto(fd, message, length, flags, addr, addr_len) {
  try {
  
      var sock = getSocketFromFD(fd);
      var dest = getSocketAddress(addr, addr_len, true);
      if (!dest) {
        // send, no address provided
        return FS.write(sock.stream, HEAP8,message, length);
      } else {
        // sendto an address
        return sock.sock_ops.sendmsg(sock, HEAP8,message, length, dest.addr, dest.port);
      }
    } catch (e) {
    if (typeof FS == 'undefined' || !(e instanceof FS.ErrnoError)) throw e;
    return -e.errno;
  }
  }

  function ___syscall_socket(domain, type, protocol) {
  try {
  
      var sock = SOCKFS.createSocket(domain, type, protocol);
      return sock.stream.fd;
    } catch (e) {
    if (typeof FS == 'undefined' || !(e instanceof FS.ErrnoError)) throw e;
    return -e.errno;
  }
  }

  function ___syscall_stat64(path, buf) {
  try {
  
      path = SYSCALLS.getStr(path);
      return SYSCALLS.doStat(FS.stat, path, buf);
    } catch (e) {
    if (typeof FS == 'undefined' || !(e instanceof FS.ErrnoError)) throw e;
    return -e.errno;
  }
  }

  function ___syscall_unlinkat(dirfd, path, flags) {
  try {
  
      path = SYSCALLS.getStr(path);
      path = SYSCALLS.calculateAt(dirfd, path);
      if (flags === 0) {
        FS.unlink(path);
      } else if (flags === 512) {
        FS.rmdir(path);
      } else {
        abort('Invalid flags passed to unlinkat');
      }
      return 0;
    } catch (e) {
    if (typeof FS == 'undefined' || !(e instanceof FS.ErrnoError)) throw e;
    return -e.errno;
  }
  }

  function __emscripten_date_now() {
      return Date.now();
    }

  var nowIsMonotonic = true;;
  function __emscripten_get_now_is_monotonic() {
      return nowIsMonotonic;
    }

  function __gmtime_js(time, tmPtr) {
      var date = new Date(HEAP32[((time)>>2)]*1000);
      HEAP32[((tmPtr)>>2)] = date.getUTCSeconds();
      HEAP32[(((tmPtr)+(4))>>2)] = date.getUTCMinutes();
      HEAP32[(((tmPtr)+(8))>>2)] = date.getUTCHours();
      HEAP32[(((tmPtr)+(12))>>2)] = date.getUTCDate();
      HEAP32[(((tmPtr)+(16))>>2)] = date.getUTCMonth();
      HEAP32[(((tmPtr)+(20))>>2)] = date.getUTCFullYear()-1900;
      HEAP32[(((tmPtr)+(24))>>2)] = date.getUTCDay();
      var start = Date.UTC(date.getUTCFullYear(), 0, 1, 0, 0, 0, 0);
      var yday = ((date.getTime() - start) / (1000 * 60 * 60 * 24))|0;
      HEAP32[(((tmPtr)+(28))>>2)] = yday;
    }

  function __localtime_js(time, tmPtr) {
      var date = new Date(HEAP32[((time)>>2)]*1000);
      HEAP32[((tmPtr)>>2)] = date.getSeconds();
      HEAP32[(((tmPtr)+(4))>>2)] = date.getMinutes();
      HEAP32[(((tmPtr)+(8))>>2)] = date.getHours();
      HEAP32[(((tmPtr)+(12))>>2)] = date.getDate();
      HEAP32[(((tmPtr)+(16))>>2)] = date.getMonth();
      HEAP32[(((tmPtr)+(20))>>2)] = date.getFullYear()-1900;
      HEAP32[(((tmPtr)+(24))>>2)] = date.getDay();
  
      var start = new Date(date.getFullYear(), 0, 1);
      var yday = ((date.getTime() - start.getTime()) / (1000 * 60 * 60 * 24))|0;
      HEAP32[(((tmPtr)+(28))>>2)] = yday;
      HEAP32[(((tmPtr)+(36))>>2)] = -(date.getTimezoneOffset() * 60);
  
      // Attention: DST is in December in South, and some regions don't have DST at all.
      var summerOffset = new Date(date.getFullYear(), 6, 1).getTimezoneOffset();
      var winterOffset = start.getTimezoneOffset();
      var dst = (summerOffset != winterOffset && date.getTimezoneOffset() == Math.min(winterOffset, summerOffset))|0;
      HEAP32[(((tmPtr)+(32))>>2)] = dst;
    }

  function __mmap_js(len, prot, flags, fd, off, allocated) {
  try {
  
      var stream = FS.getStream(fd);
      if (!stream) return -8;
      var res = FS.mmap(stream, len, off, prot, flags);
      var ptr = res.ptr;
      HEAP32[((allocated)>>2)] = res.allocated;
      return ptr;
    } catch (e) {
    if (typeof FS == 'undefined' || !(e instanceof FS.ErrnoError)) throw e;
    return -e.errno;
  }
  }

  function __munmap_js(addr, len, prot, flags, fd, offset) {
  try {
  
      var stream = FS.getStream(fd);
      if (stream) {
        if (prot & 2) {
          SYSCALLS.doMsync(addr, stream, len, flags, offset);
        }
        FS.munmap(stream);
      }
    } catch (e) {
    if (typeof FS == 'undefined' || !(e instanceof FS.ErrnoError)) throw e;
    return -e.errno;
  }
  }

  function _tzset_impl(timezone, daylight, tzname) {
      var currentYear = new Date().getFullYear();
      var winter = new Date(currentYear, 0, 1);
      var summer = new Date(currentYear, 6, 1);
      var winterOffset = winter.getTimezoneOffset();
      var summerOffset = summer.getTimezoneOffset();
  
      // Local standard timezone offset. Local standard time is not adjusted for daylight savings.
      // This code uses the fact that getTimezoneOffset returns a greater value during Standard Time versus Daylight Saving Time (DST).
      // Thus it determines the expected output during Standard Time, and it compares whether the output of the given date the same (Standard) or less (DST).
      var stdTimezoneOffset = Math.max(winterOffset, summerOffset);
  
      // timezone is specified as seconds west of UTC ("The external variable
      // `timezone` shall be set to the difference, in seconds, between
      // Coordinated Universal Time (UTC) and local standard time."), the same
      // as returned by stdTimezoneOffset.
      // See http://pubs.opengroup.org/onlinepubs/009695399/functions/tzset.html
      HEAP32[((timezone)>>2)] = stdTimezoneOffset * 60;
  
      HEAP32[((daylight)>>2)] = Number(winterOffset != summerOffset);
  
      function extractZone(date) {
        var match = date.toTimeString().match(/\(([A-Za-z ]+)\)$/);
        return match ? match[1] : "GMT";
      };
      var winterName = extractZone(winter);
      var summerName = extractZone(summer);
      var winterNamePtr = allocateUTF8(winterName);
      var summerNamePtr = allocateUTF8(summerName);
      if (summerOffset < winterOffset) {
        // Northern hemisphere
        HEAPU32[((tzname)>>2)] = winterNamePtr;
        HEAPU32[(((tzname)+(4))>>2)] = summerNamePtr;
      } else {
        HEAPU32[((tzname)>>2)] = summerNamePtr;
        HEAPU32[(((tzname)+(4))>>2)] = winterNamePtr;
      }
    }
  function __tzset_js(timezone, daylight, tzname) {
      // TODO: Use (malleable) environment variables instead of system settings.
      if (__tzset_js.called) return;
      __tzset_js.called = true;
      _tzset_impl(timezone, daylight, tzname);
    }

  function _abort() {
      abort('');
    }

  var DOTNETENTROPY = {batchedQuotaMax:65536,getBatchedRandomValues:function (buffer, bufferLength) {
              // Chrome doesn't want SharedArrayBuffer to be passed to crypto APIs
              const needTempBuf = typeof SharedArrayBuffer !== 'undefined' && Module.HEAPU8.buffer instanceof SharedArrayBuffer;
              // if we need a temporary buffer, make one that is big enough and write into it from the beginning
              // otherwise, use the wasm instance memory and write at the given 'buffer' pointer offset.
              const buf = needTempBuf ? new ArrayBuffer(bufferLength) : Module.HEAPU8.buffer;
              const offset = needTempBuf ? 0 : buffer;
              // for modern web browsers
              // map the work array to the memory buffer passed with the length
              for (let i = 0; i < bufferLength; i += this.batchedQuotaMax) {
                  const view = new Uint8Array(buf, offset + i, Math.min(bufferLength - i, this.batchedQuotaMax));
                  crypto.getRandomValues(view)
              }
              if (needTempBuf) {
                  // copy data out of the temporary buffer into the wasm instance memory
                  const heapView = new Uint8Array(Module.HEAPU8.buffer, buffer, bufferLength);
                  heapView.set(new Uint8Array (buf));
              }
          }};
  function _dotnet_browser_entropy(buffer, bufferLength) {
          // check that we have crypto available
          if (typeof crypto === 'object' && typeof crypto['getRandomValues'] === 'function') {
              DOTNETENTROPY.getBatchedRandomValues(buffer, bufferLength)
              return 0;
          } else {
              // we couldn't find a proper implementation, as Math.random() is not suitable
              // instead of aborting here we will return and let managed code handle the message
              return -1;
          }
      }

  var JSEvents = {inEventHandler:0,removeAllEventListeners:function() {
        for (var i = JSEvents.eventHandlers.length-1; i >= 0; --i) {
          JSEvents._removeHandler(i);
        }
        JSEvents.eventHandlers = [];
        JSEvents.deferredCalls = [];
      },registerRemoveEventListeners:function() {
        if (!JSEvents.removeEventListenersRegistered) {
          __ATEXIT__.push(JSEvents.removeAllEventListeners);
          JSEvents.removeEventListenersRegistered = true;
        }
      },deferredCalls:[],deferCall:function(targetFunction, precedence, argsList) {
        function arraysHaveEqualContent(arrA, arrB) {
          if (arrA.length != arrB.length) return false;
  
          for (var i in arrA) {
            if (arrA[i] != arrB[i]) return false;
          }
          return true;
        }
        // Test if the given call was already queued, and if so, don't add it again.
        for (var i in JSEvents.deferredCalls) {
          var call = JSEvents.deferredCalls[i];
          if (call.targetFunction == targetFunction && arraysHaveEqualContent(call.argsList, argsList)) {
            return;
          }
        }
        JSEvents.deferredCalls.push({
          targetFunction: targetFunction,
          precedence: precedence,
          argsList: argsList
        });
  
        JSEvents.deferredCalls.sort(function(x,y) { return x.precedence < y.precedence; });
      },removeDeferredCalls:function(targetFunction) {
        for (var i = 0; i < JSEvents.deferredCalls.length; ++i) {
          if (JSEvents.deferredCalls[i].targetFunction == targetFunction) {
            JSEvents.deferredCalls.splice(i, 1);
            --i;
          }
        }
      },canPerformEventHandlerRequests:function() {
        return JSEvents.inEventHandler && JSEvents.currentEventHandler.allowsDeferredCalls;
      },runDeferredCalls:function() {
        if (!JSEvents.canPerformEventHandlerRequests()) {
          return;
        }
        for (var i = 0; i < JSEvents.deferredCalls.length; ++i) {
          var call = JSEvents.deferredCalls[i];
          JSEvents.deferredCalls.splice(i, 1);
          --i;
          call.targetFunction.apply(null, call.argsList);
        }
      },eventHandlers:[],removeAllHandlersOnTarget:function(target, eventTypeString) {
        for (var i = 0; i < JSEvents.eventHandlers.length; ++i) {
          if (JSEvents.eventHandlers[i].target == target && 
            (!eventTypeString || eventTypeString == JSEvents.eventHandlers[i].eventTypeString)) {
             JSEvents._removeHandler(i--);
           }
        }
      },_removeHandler:function(i) {
        var h = JSEvents.eventHandlers[i];
        h.target.removeEventListener(h.eventTypeString, h.eventListenerFunc, h.useCapture);
        JSEvents.eventHandlers.splice(i, 1);
      },registerOrRemoveHandler:function(eventHandler) {
        var jsEventHandler = function jsEventHandler(event) {
          // Increment nesting count for the event handler.
          ++JSEvents.inEventHandler;
          JSEvents.currentEventHandler = eventHandler;
          // Process any old deferred calls the user has placed.
          JSEvents.runDeferredCalls();
          // Process the actual event, calls back to user C code handler.
          eventHandler.handlerFunc(event);
          // Process any new deferred calls that were placed right now from this event handler.
          JSEvents.runDeferredCalls();
          // Out of event handler - restore nesting count.
          --JSEvents.inEventHandler;
        };
        
        if (eventHandler.callbackfunc) {
          eventHandler.eventListenerFunc = jsEventHandler;
          eventHandler.target.addEventListener(eventHandler.eventTypeString, jsEventHandler, eventHandler.useCapture);
          JSEvents.eventHandlers.push(eventHandler);
          JSEvents.registerRemoveEventListeners();
        } else {
          for (var i = 0; i < JSEvents.eventHandlers.length; ++i) {
            if (JSEvents.eventHandlers[i].target == eventHandler.target
             && JSEvents.eventHandlers[i].eventTypeString == eventHandler.eventTypeString) {
               JSEvents._removeHandler(i--);
             }
          }
        }
      },getNodeNameForTarget:function(target) {
        if (!target) return '';
        if (target == window) return '#window';
        if (target == screen) return '#screen';
        return (target && target.nodeName) ? target.nodeName : '';
      },fullscreenEnabled:function() {
        return document.fullscreenEnabled
        // Safari 13.0.3 on macOS Catalina 10.15.1 still ships with prefixed webkitFullscreenEnabled.
        // TODO: If Safari at some point ships with unprefixed version, update the version check above.
        || document.webkitFullscreenEnabled
         ;
      }};
  
  function requestPointerLock(target) {
      if (target.requestPointerLock) {
        target.requestPointerLock();
      } else if (target.msRequestPointerLock) {
        target.msRequestPointerLock();
      } else {
        // document.body is known to accept pointer lock, so use that to differentiate if the user passed a bad element,
        // or if the whole browser just doesn't support the feature.
        if (document.body.requestPointerLock
          || document.body.msRequestPointerLock
          ) {
          return -3;
        } else {
          return -1;
        }
      }
      return 0;
    }
  function _emscripten_exit_pointerlock() {
      // Make sure no queued up calls will fire after this.
      JSEvents.removeDeferredCalls(requestPointerLock);
  
      if (document.exitPointerLock) {
        document.exitPointerLock();
      } else if (document.msExitPointerLock) {
        document.msExitPointerLock();
      } else {
        return -1;
      }
      return 0;
    }

  function maybeCStringToJsString(cString) {
      // "cString > 2" checks if the input is a number, and isn't of the special
      // values we accept here, EMSCRIPTEN_EVENT_TARGET_* (which map to 0, 1, 2).
      // In other words, if cString > 2 then it's a pointer to a valid place in
      // memory, and points to a C string.
      return cString > 2 ? UTF8ToString(cString) : cString;
    }
  
  var specialHTMLTargets = [0, typeof document != 'undefined' ? document : 0, typeof window != 'undefined' ? window : 0];
  function findEventTarget(target) {
      target = maybeCStringToJsString(target);
      var domElement = specialHTMLTargets[target] || (typeof document != 'undefined' ? document.querySelector(target) : undefined);
      return domElement;
    }
  
  function getBoundingClientRect(e) {
      return specialHTMLTargets.indexOf(e) < 0 ? e.getBoundingClientRect() : {'left':0,'top':0};
    }
  function _emscripten_get_element_css_size(target, width, height) {
      target = findEventTarget(target);
      if (!target) return -4;
  
      var rect = getBoundingClientRect(target);
      HEAPF64[((width)>>3)] = rect.width;
      HEAPF64[((height)>>3)] = rect.height;
  
      return 0;
    }

  function fillGamepadEventData(eventStruct, e) {
      HEAPF64[((eventStruct)>>3)] = e.timestamp;
      for (var i = 0; i < e.axes.length; ++i) {
        HEAPF64[(((eventStruct+i*8)+(16))>>3)] = e.axes[i];
      }
      for (var i = 0; i < e.buttons.length; ++i) {
        if (typeof e.buttons[i] == 'object') {
          HEAPF64[(((eventStruct+i*8)+(528))>>3)] = e.buttons[i].value;
        } else {
          HEAPF64[(((eventStruct+i*8)+(528))>>3)] = e.buttons[i];
        }
      }
      for (var i = 0; i < e.buttons.length; ++i) {
        if (typeof e.buttons[i] == 'object') {
          HEAP32[(((eventStruct+i*4)+(1040))>>2)] = e.buttons[i].pressed;
        } else {
          // Assigning a boolean to HEAP32, that's ok, but Closure would like to warn about it:
          /** @suppress {checkTypes} */
          HEAP32[(((eventStruct+i*4)+(1040))>>2)] = e.buttons[i] == 1;
        }
      }
      HEAP32[(((eventStruct)+(1296))>>2)] = e.connected;
      HEAP32[(((eventStruct)+(1300))>>2)] = e.index;
      HEAP32[(((eventStruct)+(8))>>2)] = e.axes.length;
      HEAP32[(((eventStruct)+(12))>>2)] = e.buttons.length;
      stringToUTF8(e.id, eventStruct + 1304, 64);
      stringToUTF8(e.mapping, eventStruct + 1368, 64);
    }
  function _emscripten_get_gamepad_status(index, gamepadState) {
  
      // INVALID_PARAM is returned on a Gamepad index that never was there.
      if (index < 0 || index >= JSEvents.lastGamepadState.length) return -5;
  
      // NO_DATA is returned on a Gamepad index that was removed.
      // For previously disconnected gamepads there should be an empty slot (null/undefined/false) at the index.
      // This is because gamepads must keep their original position in the array.
      // For example, removing the first of two gamepads produces [null/undefined/false, gamepad].
      if (!JSEvents.lastGamepadState[index]) return -7;
  
      fillGamepadEventData(gamepadState, JSEvents.lastGamepadState[index]);
      return 0;
    }

  function getHeapMax() {
      // Stay one Wasm page short of 4GB: while e.g. Chrome is able to allocate
      // full 4GB Wasm memories, the size will wrap back to 0 bytes in Wasm side
      // for any code that deals with heap sizes, which would require special
      // casing all heap size related code to treat 0 specially.
      return 2147483648;
    }
  function _emscripten_get_heap_max() {
      return getHeapMax();
    }

  var _emscripten_get_now;if (ENVIRONMENT_IS_NODE) {
    _emscripten_get_now = () => {
      var t = process['hrtime']();
      return t[0] * 1e3 + t[1] / 1e6;
    };
  } else if (typeof dateNow != 'undefined') {
    _emscripten_get_now = dateNow;
  } else _emscripten_get_now = () => performance.now();
  ;

  function _emscripten_get_now_res() { // return resolution of get_now, in nanoseconds
      if (ENVIRONMENT_IS_NODE) {
        return 1; // nanoseconds
      } else
      if (typeof dateNow != 'undefined') {
        return 1000; // microseconds (1/1000 of a millisecond)
      } else
      // Modern environment where performance.now() is supported:
      return 1000; // microseconds (1/1000 of a millisecond)
    }

  function _emscripten_get_num_gamepads() {
      // N.B. Do not call emscripten_get_num_gamepads() unless having first called emscripten_sample_gamepad_data(), and that has returned EMSCRIPTEN_RESULT_SUCCESS.
      // Otherwise the following line will throw an exception.
      return JSEvents.lastGamepadState.length;
    }

  function __webgl_enable_ANGLE_instanced_arrays(ctx) {
      // Extension available in WebGL 1 from Firefox 26 and Google Chrome 30 onwards. Core feature in WebGL 2.
      var ext = ctx.getExtension('ANGLE_instanced_arrays');
      if (ext) {
        ctx['vertexAttribDivisor'] = function(index, divisor) { ext['vertexAttribDivisorANGLE'](index, divisor); };
        ctx['drawArraysInstanced'] = function(mode, first, count, primcount) { ext['drawArraysInstancedANGLE'](mode, first, count, primcount); };
        ctx['drawElementsInstanced'] = function(mode, count, type, indices, primcount) { ext['drawElementsInstancedANGLE'](mode, count, type, indices, primcount); };
        return 1;
      }
    }
  
  function __webgl_enable_OES_vertex_array_object(ctx) {
      // Extension available in WebGL 1 from Firefox 25 and WebKit 536.28/desktop Safari 6.0.3 onwards. Core feature in WebGL 2.
      var ext = ctx.getExtension('OES_vertex_array_object');
      if (ext) {
        ctx['createVertexArray'] = function() { return ext['createVertexArrayOES'](); };
        ctx['deleteVertexArray'] = function(vao) { ext['deleteVertexArrayOES'](vao); };
        ctx['bindVertexArray'] = function(vao) { ext['bindVertexArrayOES'](vao); };
        ctx['isVertexArray'] = function(vao) { return ext['isVertexArrayOES'](vao); };
        return 1;
      }
    }
  
  function __webgl_enable_WEBGL_draw_buffers(ctx) {
      // Extension available in WebGL 1 from Firefox 28 onwards. Core feature in WebGL 2.
      var ext = ctx.getExtension('WEBGL_draw_buffers');
      if (ext) {
        ctx['drawBuffers'] = function(n, bufs) { ext['drawBuffersWEBGL'](n, bufs); };
        return 1;
      }
    }
  
  function __webgl_enable_WEBGL_multi_draw(ctx) {
      // Closure is expected to be allowed to minify the '.multiDrawWebgl' property, so not accessing it quoted.
      return !!(ctx.multiDrawWebgl = ctx.getExtension('WEBGL_multi_draw'));
    }
  var GL = {counter:1,buffers:[],programs:[],framebuffers:[],renderbuffers:[],textures:[],shaders:[],vaos:[],contexts:[],offscreenCanvases:{},queries:[],stringCache:{},unpackAlignment:4,recordError:function recordError(errorCode) {
        if (!GL.lastError) {
          GL.lastError = errorCode;
        }
      },getNewId:function(table) {
        var ret = GL.counter++;
        for (var i = table.length; i < ret; i++) {
          table[i] = null;
        }
        return ret;
      },getSource:function(shader, count, string, length) {
        var source = '';
        for (var i = 0; i < count; ++i) {
          var len = length ? HEAP32[(((length)+(i*4))>>2)] : -1;
          source += UTF8ToString(HEAP32[(((string)+(i*4))>>2)], len < 0 ? undefined : len);
        }
        return source;
      },createContext:function(/** @type {HTMLCanvasElement} */ canvas, webGLContextAttributes) {
  
        // BUG: Workaround Safari WebGL issue: After successfully acquiring WebGL context on a canvas,
        // calling .getContext() will always return that context independent of which 'webgl' or 'webgl2'
        // context version was passed. See https://bugs.webkit.org/show_bug.cgi?id=222758 and
        // https://github.com/emscripten-core/emscripten/issues/13295.
        // TODO: Once the bug is fixed and shipped in Safari, adjust the Safari version field in above check.
        if (!canvas.getContextSafariWebGL2Fixed) {
          canvas.getContextSafariWebGL2Fixed = canvas.getContext;
          /** @type {function(this:HTMLCanvasElement, string, (Object|null)=): (Object|null)} */
          function fixedGetContext(ver, attrs) {
            var gl = canvas.getContextSafariWebGL2Fixed(ver, attrs);
            return ((ver == 'webgl') == (gl instanceof WebGLRenderingContext)) ? gl : null;
          }
          canvas.getContext = fixedGetContext;
        }
  
        var ctx = 
          (canvas.getContext("webgl", webGLContextAttributes)
            // https://caniuse.com/#feat=webgl
            );
  
        if (!ctx) return 0;
  
        var handle = GL.registerContext(ctx, webGLContextAttributes);
  
        return handle;
      },registerContext:function(ctx, webGLContextAttributes) {
        // without pthreads a context is just an integer ID
        var handle = GL.getNewId(GL.contexts);
  
        var context = {
          handle: handle,
          attributes: webGLContextAttributes,
          version: webGLContextAttributes.majorVersion,
          GLctx: ctx
        };
  
        // Store the created context object so that we can access the context given a canvas without having to pass the parameters again.
        if (ctx.canvas) ctx.canvas.GLctxObject = context;
        GL.contexts[handle] = context;
        if (typeof webGLContextAttributes.enableExtensionsByDefault == 'undefined' || webGLContextAttributes.enableExtensionsByDefault) {
          GL.initExtensions(context);
        }
  
        return handle;
      },makeContextCurrent:function(contextHandle) {
  
        GL.currentContext = GL.contexts[contextHandle]; // Active Emscripten GL layer context object.
        Module.ctx = GLctx = GL.currentContext && GL.currentContext.GLctx; // Active WebGL context object.
        return !(contextHandle && !GLctx);
      },getContext:function(contextHandle) {
        return GL.contexts[contextHandle];
      },deleteContext:function(contextHandle) {
        if (GL.currentContext === GL.contexts[contextHandle]) GL.currentContext = null;
        if (typeof JSEvents == 'object') JSEvents.removeAllHandlersOnTarget(GL.contexts[contextHandle].GLctx.canvas); // Release all JS event handlers on the DOM element that the GL context is associated with since the context is now deleted.
        if (GL.contexts[contextHandle] && GL.contexts[contextHandle].GLctx.canvas) GL.contexts[contextHandle].GLctx.canvas.GLctxObject = undefined; // Make sure the canvas object no longer refers to the context object so there are no GC surprises.
        GL.contexts[contextHandle] = null;
      },initExtensions:function(context) {
        // If this function is called without a specific context object, init the extensions of the currently active context.
        if (!context) context = GL.currentContext;
  
        if (context.initExtensionsDone) return;
        context.initExtensionsDone = true;
  
        var GLctx = context.GLctx;
  
        // Detect the presence of a few extensions manually, this GL interop layer itself will need to know if they exist.
  
        // Extensions that are only available in WebGL 1 (the calls will be no-ops if called on a WebGL 2 context active)
        __webgl_enable_ANGLE_instanced_arrays(GLctx);
        __webgl_enable_OES_vertex_array_object(GLctx);
        __webgl_enable_WEBGL_draw_buffers(GLctx);
  
        {
          GLctx.disjointTimerQueryExt = GLctx.getExtension("EXT_disjoint_timer_query");
        }
  
        __webgl_enable_WEBGL_multi_draw(GLctx);
  
        // .getSupportedExtensions() can return null if context is lost, so coerce to empty array.
        var exts = GLctx.getSupportedExtensions() || [];
        exts.forEach(function(ext) {
          // WEBGL_lose_context, WEBGL_debug_renderer_info and WEBGL_debug_shaders are not enabled by default.
          if (!ext.includes('lose_context') && !ext.includes('debug')) {
            // Call .getExtension() to enable that extension permanently.
            GLctx.getExtension(ext);
          }
        });
      }};
  function _emscripten_glActiveTexture(x0) { GLctx['activeTexture'](x0) }

  function _emscripten_glAttachShader(program, shader) {
      GLctx.attachShader(GL.programs[program], GL.shaders[shader]);
    }

  function _emscripten_glBeginQueryEXT(target, id) {
      GLctx.disjointTimerQueryExt['beginQueryEXT'](target, GL.queries[id]);
    }

  function _emscripten_glBindAttribLocation(program, index, name) {
      GLctx.bindAttribLocation(GL.programs[program], index, UTF8ToString(name));
    }

  function _emscripten_glBindBuffer(target, buffer) {
  
      GLctx.bindBuffer(target, GL.buffers[buffer]);
    }

  function _emscripten_glBindFramebuffer(target, framebuffer) {
  
      GLctx.bindFramebuffer(target, GL.framebuffers[framebuffer]);
  
    }

  function _emscripten_glBindRenderbuffer(target, renderbuffer) {
      GLctx.bindRenderbuffer(target, GL.renderbuffers[renderbuffer]);
    }

  function _emscripten_glBindTexture(target, texture) {
      GLctx.bindTexture(target, GL.textures[texture]);
    }

  function _emscripten_glBindVertexArrayOES(vao) {
      GLctx['bindVertexArray'](GL.vaos[vao]);
    }

  function _emscripten_glBlendColor(x0, x1, x2, x3) { GLctx['blendColor'](x0, x1, x2, x3) }

  function _emscripten_glBlendEquation(x0) { GLctx['blendEquation'](x0) }

  function _emscripten_glBlendEquationSeparate(x0, x1) { GLctx['blendEquationSeparate'](x0, x1) }

  function _emscripten_glBlendFunc(x0, x1) { GLctx['blendFunc'](x0, x1) }

  function _emscripten_glBlendFuncSeparate(x0, x1, x2, x3) { GLctx['blendFuncSeparate'](x0, x1, x2, x3) }

  function _emscripten_glBufferData(target, size, data, usage) {
  
        // N.b. here first form specifies a heap subarray, second form an integer size, so the ?: code here is polymorphic. It is advised to avoid
        // randomly mixing both uses in calling code, to avoid any potential JS engine JIT issues.
        GLctx.bufferData(target, data ? HEAPU8.subarray(data, data+size) : size, usage);
    }

  function _emscripten_glBufferSubData(target, offset, size, data) {
      GLctx.bufferSubData(target, offset, HEAPU8.subarray(data, data+size));
    }

  function _emscripten_glCheckFramebufferStatus(x0) { return GLctx['checkFramebufferStatus'](x0) }

  function _emscripten_glClear(x0) { GLctx['clear'](x0) }

  function _emscripten_glClearColor(x0, x1, x2, x3) { GLctx['clearColor'](x0, x1, x2, x3) }

  function _emscripten_glClearDepthf(x0) { GLctx['clearDepth'](x0) }

  function _emscripten_glClearStencil(x0) { GLctx['clearStencil'](x0) }

  function _emscripten_glColorMask(red, green, blue, alpha) {
      GLctx.colorMask(!!red, !!green, !!blue, !!alpha);
    }

  function _emscripten_glCompileShader(shader) {
      GLctx.compileShader(GL.shaders[shader]);
    }

  function _emscripten_glCompressedTexImage2D(target, level, internalFormat, width, height, border, imageSize, data) {
      GLctx['compressedTexImage2D'](target, level, internalFormat, width, height, border, data ? HEAPU8.subarray((data), (data+imageSize)) : null);
    }

  function _emscripten_glCompressedTexSubImage2D(target, level, xoffset, yoffset, width, height, format, imageSize, data) {
      GLctx['compressedTexSubImage2D'](target, level, xoffset, yoffset, width, height, format, data ? HEAPU8.subarray((data), (data+imageSize)) : null);
    }

  function _emscripten_glCopyTexImage2D(x0, x1, x2, x3, x4, x5, x6, x7) { GLctx['copyTexImage2D'](x0, x1, x2, x3, x4, x5, x6, x7) }

  function _emscripten_glCopyTexSubImage2D(x0, x1, x2, x3, x4, x5, x6, x7) { GLctx['copyTexSubImage2D'](x0, x1, x2, x3, x4, x5, x6, x7) }

  function _emscripten_glCreateProgram() {
      var id = GL.getNewId(GL.programs);
      var program = GLctx.createProgram();
      // Store additional information needed for each shader program:
      program.name = id;
      // Lazy cache results of glGetProgramiv(GL_ACTIVE_UNIFORM_MAX_LENGTH/GL_ACTIVE_ATTRIBUTE_MAX_LENGTH/GL_ACTIVE_UNIFORM_BLOCK_MAX_NAME_LENGTH)
      program.maxUniformLength = program.maxAttributeLength = program.maxUniformBlockNameLength = 0;
      program.uniformIdCounter = 1;
      GL.programs[id] = program;
      return id;
    }

  function _emscripten_glCreateShader(shaderType) {
      var id = GL.getNewId(GL.shaders);
      GL.shaders[id] = GLctx.createShader(shaderType);
  
      return id;
    }

  function _emscripten_glCullFace(x0) { GLctx['cullFace'](x0) }

  function _emscripten_glDeleteBuffers(n, buffers) {
      for (var i = 0; i < n; i++) {
        var id = HEAP32[(((buffers)+(i*4))>>2)];
        var buffer = GL.buffers[id];
  
        // From spec: "glDeleteBuffers silently ignores 0's and names that do not
        // correspond to existing buffer objects."
        if (!buffer) continue;
  
        GLctx.deleteBuffer(buffer);
        buffer.name = 0;
        GL.buffers[id] = null;
  
      }
    }

  function _emscripten_glDeleteFramebuffers(n, framebuffers) {
      for (var i = 0; i < n; ++i) {
        var id = HEAP32[(((framebuffers)+(i*4))>>2)];
        var framebuffer = GL.framebuffers[id];
        if (!framebuffer) continue; // GL spec: "glDeleteFramebuffers silently ignores 0s and names that do not correspond to existing framebuffer objects".
        GLctx.deleteFramebuffer(framebuffer);
        framebuffer.name = 0;
        GL.framebuffers[id] = null;
      }
    }

  function _emscripten_glDeleteProgram(id) {
      if (!id) return;
      var program = GL.programs[id];
      if (!program) { // glDeleteProgram actually signals an error when deleting a nonexisting object, unlike some other GL delete functions.
        GL.recordError(0x501 /* GL_INVALID_VALUE */);
        return;
      }
      GLctx.deleteProgram(program);
      program.name = 0;
      GL.programs[id] = null;
    }

  function _emscripten_glDeleteQueriesEXT(n, ids) {
      for (var i = 0; i < n; i++) {
        var id = HEAP32[(((ids)+(i*4))>>2)];
        var query = GL.queries[id];
        if (!query) continue; // GL spec: "unused names in ids are ignored, as is the name zero."
        GLctx.disjointTimerQueryExt['deleteQueryEXT'](query);
        GL.queries[id] = null;
      }
    }

  function _emscripten_glDeleteRenderbuffers(n, renderbuffers) {
      for (var i = 0; i < n; i++) {
        var id = HEAP32[(((renderbuffers)+(i*4))>>2)];
        var renderbuffer = GL.renderbuffers[id];
        if (!renderbuffer) continue; // GL spec: "glDeleteRenderbuffers silently ignores 0s and names that do not correspond to existing renderbuffer objects".
        GLctx.deleteRenderbuffer(renderbuffer);
        renderbuffer.name = 0;
        GL.renderbuffers[id] = null;
      }
    }

  function _emscripten_glDeleteShader(id) {
      if (!id) return;
      var shader = GL.shaders[id];
      if (!shader) { // glDeleteShader actually signals an error when deleting a nonexisting object, unlike some other GL delete functions.
        GL.recordError(0x501 /* GL_INVALID_VALUE */);
        return;
      }
      GLctx.deleteShader(shader);
      GL.shaders[id] = null;
    }

  function _emscripten_glDeleteTextures(n, textures) {
      for (var i = 0; i < n; i++) {
        var id = HEAP32[(((textures)+(i*4))>>2)];
        var texture = GL.textures[id];
        if (!texture) continue; // GL spec: "glDeleteTextures silently ignores 0s and names that do not correspond to existing textures".
        GLctx.deleteTexture(texture);
        texture.name = 0;
        GL.textures[id] = null;
      }
    }

  function _emscripten_glDeleteVertexArraysOES(n, vaos) {
      for (var i = 0; i < n; i++) {
        var id = HEAP32[(((vaos)+(i*4))>>2)];
        GLctx['deleteVertexArray'](GL.vaos[id]);
        GL.vaos[id] = null;
      }
    }

  function _emscripten_glDepthFunc(x0) { GLctx['depthFunc'](x0) }

  function _emscripten_glDepthMask(flag) {
      GLctx.depthMask(!!flag);
    }

  function _emscripten_glDepthRangef(x0, x1) { GLctx['depthRange'](x0, x1) }

  function _emscripten_glDetachShader(program, shader) {
      GLctx.detachShader(GL.programs[program], GL.shaders[shader]);
    }

  function _emscripten_glDisable(x0) { GLctx['disable'](x0) }

  function _emscripten_glDisableVertexAttribArray(index) {
      GLctx.disableVertexAttribArray(index);
    }

  function _emscripten_glDrawArrays(mode, first, count) {
  
      GLctx.drawArrays(mode, first, count);
  
    }

  function _emscripten_glDrawArraysInstancedANGLE(mode, first, count, primcount) {
      GLctx['drawArraysInstanced'](mode, first, count, primcount);
    }

  var tempFixedLengthArray = [];
  function _emscripten_glDrawBuffersWEBGL(n, bufs) {
  
      var bufArray = tempFixedLengthArray[n];
      for (var i = 0; i < n; i++) {
        bufArray[i] = HEAP32[(((bufs)+(i*4))>>2)];
      }
  
      GLctx['drawBuffers'](bufArray);
    }

  function _emscripten_glDrawElements(mode, count, type, indices) {
  
      GLctx.drawElements(mode, count, type, indices);
  
    }

  function _emscripten_glDrawElementsInstancedANGLE(mode, count, type, indices, primcount) {
      GLctx['drawElementsInstanced'](mode, count, type, indices, primcount);
    }

  function _emscripten_glEnable(x0) { GLctx['enable'](x0) }

  function _emscripten_glEnableVertexAttribArray(index) {
      GLctx.enableVertexAttribArray(index);
    }

  function _emscripten_glEndQueryEXT(target) {
      GLctx.disjointTimerQueryExt['endQueryEXT'](target);
    }

  function _emscripten_glFinish() { GLctx['finish']() }

  function _emscripten_glFlush() { GLctx['flush']() }

  function _emscripten_glFramebufferRenderbuffer(target, attachment, renderbuffertarget, renderbuffer) {
      GLctx.framebufferRenderbuffer(target, attachment, renderbuffertarget,
                                         GL.renderbuffers[renderbuffer]);
    }

  function _emscripten_glFramebufferTexture2D(target, attachment, textarget, texture, level) {
      GLctx.framebufferTexture2D(target, attachment, textarget,
                                      GL.textures[texture], level);
    }

  function _emscripten_glFrontFace(x0) { GLctx['frontFace'](x0) }

  function __glGenObject(n, buffers, createFunction, objectTable
      ) {
      for (var i = 0; i < n; i++) {
        var buffer = GLctx[createFunction]();
        var id = buffer && GL.getNewId(objectTable);
        if (buffer) {
          buffer.name = id;
          objectTable[id] = buffer;
        } else {
          GL.recordError(0x502 /* GL_INVALID_OPERATION */);
        }
        HEAP32[(((buffers)+(i*4))>>2)] = id;
      }
    }
  function _emscripten_glGenBuffers(n, buffers) {
      __glGenObject(n, buffers, 'createBuffer', GL.buffers
        );
    }

  function _emscripten_glGenFramebuffers(n, ids) {
      __glGenObject(n, ids, 'createFramebuffer', GL.framebuffers
        );
    }

  function _emscripten_glGenQueriesEXT(n, ids) {
      for (var i = 0; i < n; i++) {
        var query = GLctx.disjointTimerQueryExt['createQueryEXT']();
        if (!query) {
          GL.recordError(0x502 /* GL_INVALID_OPERATION */);
          while (i < n) HEAP32[(((ids)+(i++*4))>>2)] = 0;
          return;
        }
        var id = GL.getNewId(GL.queries);
        query.name = id;
        GL.queries[id] = query;
        HEAP32[(((ids)+(i*4))>>2)] = id;
      }
    }

  function _emscripten_glGenRenderbuffers(n, renderbuffers) {
      __glGenObject(n, renderbuffers, 'createRenderbuffer', GL.renderbuffers
        );
    }

  function _emscripten_glGenTextures(n, textures) {
      __glGenObject(n, textures, 'createTexture', GL.textures
        );
    }

  function _emscripten_glGenVertexArraysOES(n, arrays) {
      __glGenObject(n, arrays, 'createVertexArray', GL.vaos
        );
    }

  function _emscripten_glGenerateMipmap(x0) { GLctx['generateMipmap'](x0) }

  function __glGetActiveAttribOrUniform(funcName, program, index, bufSize, length, size, type, name) {
      program = GL.programs[program];
      var info = GLctx[funcName](program, index);
      if (info) { // If an error occurs, nothing will be written to length, size and type and name.
        var numBytesWrittenExclNull = name && stringToUTF8(info.name, name, bufSize);
        if (length) HEAP32[((length)>>2)] = numBytesWrittenExclNull;
        if (size) HEAP32[((size)>>2)] = info.size;
        if (type) HEAP32[((type)>>2)] = info.type;
      }
    }
  function _emscripten_glGetActiveAttrib(program, index, bufSize, length, size, type, name) {
      __glGetActiveAttribOrUniform('getActiveAttrib', program, index, bufSize, length, size, type, name);
    }

  function _emscripten_glGetActiveUniform(program, index, bufSize, length, size, type, name) {
      __glGetActiveAttribOrUniform('getActiveUniform', program, index, bufSize, length, size, type, name);
    }

  function _emscripten_glGetAttachedShaders(program, maxCount, count, shaders) {
      var result = GLctx.getAttachedShaders(GL.programs[program]);
      var len = result.length;
      if (len > maxCount) {
        len = maxCount;
      }
      HEAP32[((count)>>2)] = len;
      for (var i = 0; i < len; ++i) {
        var id = GL.shaders.indexOf(result[i]);
        HEAP32[(((shaders)+(i*4))>>2)] = id;
      }
    }

  function _emscripten_glGetAttribLocation(program, name) {
      return GLctx.getAttribLocation(GL.programs[program], UTF8ToString(name));
    }

  function writeI53ToI64(ptr, num) {
      HEAPU32[ptr>>2] = num;
      HEAPU32[ptr+4>>2] = (num - HEAPU32[ptr>>2])/4294967296;
    }
  function emscriptenWebGLGet(name_, p, type) {
      // Guard against user passing a null pointer.
      // Note that GLES2 spec does not say anything about how passing a null pointer should be treated.
      // Testing on desktop core GL 3, the application crashes on glGetIntegerv to a null pointer, but
      // better to report an error instead of doing anything random.
      if (!p) {
        GL.recordError(0x501 /* GL_INVALID_VALUE */);
        return;
      }
      var ret = undefined;
      switch (name_) { // Handle a few trivial GLES values
        case 0x8DFA: // GL_SHADER_COMPILER
          ret = 1;
          break;
        case 0x8DF8: // GL_SHADER_BINARY_FORMATS
          if (type != 0 && type != 1) {
            GL.recordError(0x500); // GL_INVALID_ENUM
          }
          return; // Do not write anything to the out pointer, since no binary formats are supported.
        case 0x8DF9: // GL_NUM_SHADER_BINARY_FORMATS
          ret = 0;
          break;
        case 0x86A2: // GL_NUM_COMPRESSED_TEXTURE_FORMATS
          // WebGL doesn't have GL_NUM_COMPRESSED_TEXTURE_FORMATS (it's obsolete since GL_COMPRESSED_TEXTURE_FORMATS returns a JS array that can be queried for length),
          // so implement it ourselves to allow C++ GLES2 code get the length.
          var formats = GLctx.getParameter(0x86A3 /*GL_COMPRESSED_TEXTURE_FORMATS*/);
          ret = formats ? formats.length : 0;
          break;
  
      }
  
      if (ret === undefined) {
        var result = GLctx.getParameter(name_);
        switch (typeof result) {
          case "number":
            ret = result;
            break;
          case "boolean":
            ret = result ? 1 : 0;
            break;
          case "string":
            GL.recordError(0x500); // GL_INVALID_ENUM
            return;
          case "object":
            if (result === null) {
              // null is a valid result for some (e.g., which buffer is bound - perhaps nothing is bound), but otherwise
              // can mean an invalid name_, which we need to report as an error
              switch (name_) {
                case 0x8894: // ARRAY_BUFFER_BINDING
                case 0x8B8D: // CURRENT_PROGRAM
                case 0x8895: // ELEMENT_ARRAY_BUFFER_BINDING
                case 0x8CA6: // FRAMEBUFFER_BINDING or DRAW_FRAMEBUFFER_BINDING
                case 0x8CA7: // RENDERBUFFER_BINDING
                case 0x8069: // TEXTURE_BINDING_2D
                case 0x85B5: // WebGL 2 GL_VERTEX_ARRAY_BINDING, or WebGL 1 extension OES_vertex_array_object GL_VERTEX_ARRAY_BINDING_OES
                case 0x8514: { // TEXTURE_BINDING_CUBE_MAP
                  ret = 0;
                  break;
                }
                default: {
                  GL.recordError(0x500); // GL_INVALID_ENUM
                  return;
                }
              }
            } else if (result instanceof Float32Array ||
                       result instanceof Uint32Array ||
                       result instanceof Int32Array ||
                       result instanceof Array) {
              for (var i = 0; i < result.length; ++i) {
                switch (type) {
                  case 0: HEAP32[(((p)+(i*4))>>2)] = result[i]; break;
                  case 2: HEAPF32[(((p)+(i*4))>>2)] = result[i]; break;
                  case 4: HEAP8[(((p)+(i))>>0)] = result[i] ? 1 : 0; break;
                }
              }
              return;
            } else {
              try {
                ret = result.name | 0;
              } catch(e) {
                GL.recordError(0x500); // GL_INVALID_ENUM
                err('GL_INVALID_ENUM in glGet' + type + 'v: Unknown object returned from WebGL getParameter(' + name_ + ')! (error: ' + e + ')');
                return;
              }
            }
            break;
          default:
            GL.recordError(0x500); // GL_INVALID_ENUM
            err('GL_INVALID_ENUM in glGet' + type + 'v: Native code calling glGet' + type + 'v(' + name_ + ') and it returns ' + result + ' of type ' + typeof(result) + '!');
            return;
        }
      }
  
      switch (type) {
        case 1: writeI53ToI64(p, ret); break;
        case 0: HEAP32[((p)>>2)] = ret; break;
        case 2:   HEAPF32[((p)>>2)] = ret; break;
        case 4: HEAP8[((p)>>0)] = ret ? 1 : 0; break;
      }
    }
  function _emscripten_glGetBooleanv(name_, p) {
      emscriptenWebGLGet(name_, p, 4);
    }

  function _emscripten_glGetBufferParameteriv(target, value, data) {
      if (!data) {
        // GLES2 specification does not specify how to behave if data is a null pointer. Since calling this function does not make sense
        // if data == null, issue a GL error to notify user about it.
        GL.recordError(0x501 /* GL_INVALID_VALUE */);
        return;
      }
      HEAP32[((data)>>2)] = GLctx.getBufferParameter(target, value);
    }

  function _emscripten_glGetError() {
      var error = GLctx.getError() || GL.lastError;
      GL.lastError = 0/*GL_NO_ERROR*/;
      return error;
    }

  function _emscripten_glGetFloatv(name_, p) {
      emscriptenWebGLGet(name_, p, 2);
    }

  function _emscripten_glGetFramebufferAttachmentParameteriv(target, attachment, pname, params) {
      var result = GLctx.getFramebufferAttachmentParameter(target, attachment, pname);
      if (result instanceof WebGLRenderbuffer ||
          result instanceof WebGLTexture) {
        result = result.name | 0;
      }
      HEAP32[((params)>>2)] = result;
    }

  function _emscripten_glGetIntegerv(name_, p) {
      emscriptenWebGLGet(name_, p, 0);
    }

  function _emscripten_glGetProgramInfoLog(program, maxLength, length, infoLog) {
      var log = GLctx.getProgramInfoLog(GL.programs[program]);
      if (log === null) log = '(unknown error)';
      var numBytesWrittenExclNull = (maxLength > 0 && infoLog) ? stringToUTF8(log, infoLog, maxLength) : 0;
      if (length) HEAP32[((length)>>2)] = numBytesWrittenExclNull;
    }

  function _emscripten_glGetProgramiv(program, pname, p) {
      if (!p) {
        // GLES2 specification does not specify how to behave if p is a null pointer. Since calling this function does not make sense
        // if p == null, issue a GL error to notify user about it.
        GL.recordError(0x501 /* GL_INVALID_VALUE */);
        return;
      }
  
      if (program >= GL.counter) {
        GL.recordError(0x501 /* GL_INVALID_VALUE */);
        return;
      }
  
      program = GL.programs[program];
  
      if (pname == 0x8B84) { // GL_INFO_LOG_LENGTH
        var log = GLctx.getProgramInfoLog(program);
        if (log === null) log = '(unknown error)';
        HEAP32[((p)>>2)] = log.length + 1;
      } else if (pname == 0x8B87 /* GL_ACTIVE_UNIFORM_MAX_LENGTH */) {
        if (!program.maxUniformLength) {
          for (var i = 0; i < GLctx.getProgramParameter(program, 0x8B86/*GL_ACTIVE_UNIFORMS*/); ++i) {
            program.maxUniformLength = Math.max(program.maxUniformLength, GLctx.getActiveUniform(program, i).name.length+1);
          }
        }
        HEAP32[((p)>>2)] = program.maxUniformLength;
      } else if (pname == 0x8B8A /* GL_ACTIVE_ATTRIBUTE_MAX_LENGTH */) {
        if (!program.maxAttributeLength) {
          for (var i = 0; i < GLctx.getProgramParameter(program, 0x8B89/*GL_ACTIVE_ATTRIBUTES*/); ++i) {
            program.maxAttributeLength = Math.max(program.maxAttributeLength, GLctx.getActiveAttrib(program, i).name.length+1);
          }
        }
        HEAP32[((p)>>2)] = program.maxAttributeLength;
      } else if (pname == 0x8A35 /* GL_ACTIVE_UNIFORM_BLOCK_MAX_NAME_LENGTH */) {
        if (!program.maxUniformBlockNameLength) {
          for (var i = 0; i < GLctx.getProgramParameter(program, 0x8A36/*GL_ACTIVE_UNIFORM_BLOCKS*/); ++i) {
            program.maxUniformBlockNameLength = Math.max(program.maxUniformBlockNameLength, GLctx.getActiveUniformBlockName(program, i).length+1);
          }
        }
        HEAP32[((p)>>2)] = program.maxUniformBlockNameLength;
      } else {
        HEAP32[((p)>>2)] = GLctx.getProgramParameter(program, pname);
      }
    }

  function _emscripten_glGetQueryObjecti64vEXT(id, pname, params) {
      if (!params) {
        // GLES2 specification does not specify how to behave if params is a null pointer. Since calling this function does not make sense
        // if p == null, issue a GL error to notify user about it.
        GL.recordError(0x501 /* GL_INVALID_VALUE */);
        return;
      }
      var query = GL.queries[id];
      var param;
      {
        param = GLctx.disjointTimerQueryExt['getQueryObjectEXT'](query, pname);
      }
      var ret;
      if (typeof param == 'boolean') {
        ret = param ? 1 : 0;
      } else {
        ret = param;
      }
      writeI53ToI64(params, ret);
    }

  function _emscripten_glGetQueryObjectivEXT(id, pname, params) {
      if (!params) {
        // GLES2 specification does not specify how to behave if params is a null pointer. Since calling this function does not make sense
        // if p == null, issue a GL error to notify user about it.
        GL.recordError(0x501 /* GL_INVALID_VALUE */);
        return;
      }
      var query = GL.queries[id];
      var param = GLctx.disjointTimerQueryExt['getQueryObjectEXT'](query, pname);
      var ret;
      if (typeof param == 'boolean') {
        ret = param ? 1 : 0;
      } else {
        ret = param;
      }
      HEAP32[((params)>>2)] = ret;
    }

  function _emscripten_glGetQueryObjectui64vEXT(id, pname, params) {
      if (!params) {
        // GLES2 specification does not specify how to behave if params is a null pointer. Since calling this function does not make sense
        // if p == null, issue a GL error to notify user about it.
        GL.recordError(0x501 /* GL_INVALID_VALUE */);
        return;
      }
      var query = GL.queries[id];
      var param;
      {
        param = GLctx.disjointTimerQueryExt['getQueryObjectEXT'](query, pname);
      }
      var ret;
      if (typeof param == 'boolean') {
        ret = param ? 1 : 0;
      } else {
        ret = param;
      }
      writeI53ToI64(params, ret);
    }

  function _emscripten_glGetQueryObjectuivEXT(id, pname, params) {
      if (!params) {
        // GLES2 specification does not specify how to behave if params is a null pointer. Since calling this function does not make sense
        // if p == null, issue a GL error to notify user about it.
        GL.recordError(0x501 /* GL_INVALID_VALUE */);
        return;
      }
      var query = GL.queries[id];
      var param = GLctx.disjointTimerQueryExt['getQueryObjectEXT'](query, pname);
      var ret;
      if (typeof param == 'boolean') {
        ret = param ? 1 : 0;
      } else {
        ret = param;
      }
      HEAP32[((params)>>2)] = ret;
    }

  function _emscripten_glGetQueryivEXT(target, pname, params) {
      if (!params) {
        // GLES2 specification does not specify how to behave if params is a null pointer. Since calling this function does not make sense
        // if p == null, issue a GL error to notify user about it.
        GL.recordError(0x501 /* GL_INVALID_VALUE */);
        return;
      }
      HEAP32[((params)>>2)] = GLctx.disjointTimerQueryExt['getQueryEXT'](target, pname);
    }

  function _emscripten_glGetRenderbufferParameteriv(target, pname, params) {
      if (!params) {
        // GLES2 specification does not specify how to behave if params is a null pointer. Since calling this function does not make sense
        // if params == null, issue a GL error to notify user about it.
        GL.recordError(0x501 /* GL_INVALID_VALUE */);
        return;
      }
      HEAP32[((params)>>2)] = GLctx.getRenderbufferParameter(target, pname);
    }

  function _emscripten_glGetShaderInfoLog(shader, maxLength, length, infoLog) {
      var log = GLctx.getShaderInfoLog(GL.shaders[shader]);
      if (log === null) log = '(unknown error)';
      var numBytesWrittenExclNull = (maxLength > 0 && infoLog) ? stringToUTF8(log, infoLog, maxLength) : 0;
      if (length) HEAP32[((length)>>2)] = numBytesWrittenExclNull;
    }

  function _emscripten_glGetShaderPrecisionFormat(shaderType, precisionType, range, precision) {
      var result = GLctx.getShaderPrecisionFormat(shaderType, precisionType);
      HEAP32[((range)>>2)] = result.rangeMin;
      HEAP32[(((range)+(4))>>2)] = result.rangeMax;
      HEAP32[((precision)>>2)] = result.precision;
    }

  function _emscripten_glGetShaderSource(shader, bufSize, length, source) {
      var result = GLctx.getShaderSource(GL.shaders[shader]);
      if (!result) return; // If an error occurs, nothing will be written to length or source.
      var numBytesWrittenExclNull = (bufSize > 0 && source) ? stringToUTF8(result, source, bufSize) : 0;
      if (length) HEAP32[((length)>>2)] = numBytesWrittenExclNull;
    }

  function _emscripten_glGetShaderiv(shader, pname, p) {
      if (!p) {
        // GLES2 specification does not specify how to behave if p is a null pointer. Since calling this function does not make sense
        // if p == null, issue a GL error to notify user about it.
        GL.recordError(0x501 /* GL_INVALID_VALUE */);
        return;
      }
      if (pname == 0x8B84) { // GL_INFO_LOG_LENGTH
        var log = GLctx.getShaderInfoLog(GL.shaders[shader]);
        if (log === null) log = '(unknown error)';
        // The GLES2 specification says that if the shader has an empty info log,
        // a value of 0 is returned. Otherwise the log has a null char appended.
        // (An empty string is falsey, so we can just check that instead of
        // looking at log.length.)
        var logLength = log ? log.length + 1 : 0;
        HEAP32[((p)>>2)] = logLength;
      } else if (pname == 0x8B88) { // GL_SHADER_SOURCE_LENGTH
        var source = GLctx.getShaderSource(GL.shaders[shader]);
        // source may be a null, or the empty string, both of which are falsey
        // values that we report a 0 length for.
        var sourceLength = source ? source.length + 1 : 0;
        HEAP32[((p)>>2)] = sourceLength;
      } else {
        HEAP32[((p)>>2)] = GLctx.getShaderParameter(GL.shaders[shader], pname);
      }
    }

  function stringToNewUTF8(jsString) {
      var length = lengthBytesUTF8(jsString)+1;
      var cString = _malloc(length);
      stringToUTF8(jsString, cString, length);
      return cString;
    }
  function _emscripten_glGetString(name_) {
      var ret = GL.stringCache[name_];
      if (!ret) {
        switch (name_) {
          case 0x1F03 /* GL_EXTENSIONS */:
            var exts = GLctx.getSupportedExtensions() || []; // .getSupportedExtensions() can return null if context is lost, so coerce to empty array.
            exts = exts.concat(exts.map(function(e) { return "GL_" + e; }));
            ret = stringToNewUTF8(exts.join(' '));
            break;
          case 0x1F00 /* GL_VENDOR */:
          case 0x1F01 /* GL_RENDERER */:
          case 0x9245 /* UNMASKED_VENDOR_WEBGL */:
          case 0x9246 /* UNMASKED_RENDERER_WEBGL */:
            var s = GLctx.getParameter(name_);
            if (!s) {
              GL.recordError(0x500/*GL_INVALID_ENUM*/);
            }
            ret = s && stringToNewUTF8(s);
            break;
  
          case 0x1F02 /* GL_VERSION */:
            var glVersion = GLctx.getParameter(0x1F02 /*GL_VERSION*/);
            // return GLES version string corresponding to the version of the WebGL context
            {
              glVersion = 'OpenGL ES 2.0 (' + glVersion + ')';
            }
            ret = stringToNewUTF8(glVersion);
            break;
          case 0x8B8C /* GL_SHADING_LANGUAGE_VERSION */:
            var glslVersion = GLctx.getParameter(0x8B8C /*GL_SHADING_LANGUAGE_VERSION*/);
            // extract the version number 'N.M' from the string 'WebGL GLSL ES N.M ...'
            var ver_re = /^WebGL GLSL ES ([0-9]\.[0-9][0-9]?)(?:$| .*)/;
            var ver_num = glslVersion.match(ver_re);
            if (ver_num !== null) {
              if (ver_num[1].length == 3) ver_num[1] = ver_num[1] + '0'; // ensure minor version has 2 digits
              glslVersion = 'OpenGL ES GLSL ES ' + ver_num[1] + ' (' + glslVersion + ')';
            }
            ret = stringToNewUTF8(glslVersion);
            break;
          default:
            GL.recordError(0x500/*GL_INVALID_ENUM*/);
            // fall through
        }
        GL.stringCache[name_] = ret;
      }
      return ret;
    }

  function _emscripten_glGetTexParameterfv(target, pname, params) {
      if (!params) {
        // GLES2 specification does not specify how to behave if params is a null pointer. Since calling this function does not make sense
        // if p == null, issue a GL error to notify user about it.
        GL.recordError(0x501 /* GL_INVALID_VALUE */);
        return;
      }
      HEAPF32[((params)>>2)] = GLctx.getTexParameter(target, pname);
    }

  function _emscripten_glGetTexParameteriv(target, pname, params) {
      if (!params) {
        // GLES2 specification does not specify how to behave if params is a null pointer. Since calling this function does not make sense
        // if p == null, issue a GL error to notify user about it.
        GL.recordError(0x501 /* GL_INVALID_VALUE */);
        return;
      }
      HEAP32[((params)>>2)] = GLctx.getTexParameter(target, pname);
    }

  /** @noinline */
  function webglGetLeftBracePos(name) {
      return name.slice(-1) == ']' && name.lastIndexOf('[');
    }
  function webglPrepareUniformLocationsBeforeFirstUse(program) {
      var uniformLocsById = program.uniformLocsById, // Maps GLuint -> WebGLUniformLocation
        uniformSizeAndIdsByName = program.uniformSizeAndIdsByName, // Maps name -> [uniform array length, GLuint]
        i, j;
  
      // On the first time invocation of glGetUniformLocation on this shader program:
      // initialize cache data structures and discover which uniforms are arrays.
      if (!uniformLocsById) {
        // maps GLint integer locations to WebGLUniformLocations
        program.uniformLocsById = uniformLocsById = {};
        // maps integer locations back to uniform name strings, so that we can lazily fetch uniform array locations
        program.uniformArrayNamesById = {};
  
        for (i = 0; i < GLctx.getProgramParameter(program, 0x8B86/*GL_ACTIVE_UNIFORMS*/); ++i) {
          var u = GLctx.getActiveUniform(program, i);
          var nm = u.name;
          var sz = u.size;
          var lb = webglGetLeftBracePos(nm);
          var arrayName = lb > 0 ? nm.slice(0, lb) : nm;
  
          // Assign a new location.
          var id = program.uniformIdCounter;
          program.uniformIdCounter += sz;
          // Eagerly get the location of the uniformArray[0] base element.
          // The remaining indices >0 will be left for lazy evaluation to
          // improve performance. Those may never be needed to fetch, if the
          // application fills arrays always in full starting from the first
          // element of the array.
          uniformSizeAndIdsByName[arrayName] = [sz, id];
  
          // Store placeholder integers in place that highlight that these
          // >0 index locations are array indices pending population.
          for(j = 0; j < sz; ++j) {
            uniformLocsById[id] = j;
            program.uniformArrayNamesById[id++] = arrayName;
          }
        }
      }
    }
  function _emscripten_glGetUniformLocation(program, name) {
  
      name = UTF8ToString(name);
  
      if (program = GL.programs[program]) {
        webglPrepareUniformLocationsBeforeFirstUse(program);
        var uniformLocsById = program.uniformLocsById; // Maps GLuint -> WebGLUniformLocation
        var arrayIndex = 0;
        var uniformBaseName = name;
  
        // Invariant: when populating integer IDs for uniform locations, we must maintain the precondition that
        // arrays reside in contiguous addresses, i.e. for a 'vec4 colors[10];', colors[4] must be at location colors[0]+4.
        // However, user might call glGetUniformLocation(program, "colors") for an array, so we cannot discover based on the user
        // input arguments whether the uniform we are dealing with is an array. The only way to discover which uniforms are arrays
        // is to enumerate over all the active uniforms in the program.
        var leftBrace = webglGetLeftBracePos(name);
  
        // If user passed an array accessor "[index]", parse the array index off the accessor.
        if (leftBrace > 0) {
          arrayIndex = jstoi_q(name.slice(leftBrace + 1)) >>> 0; // "index]", coerce parseInt(']') with >>>0 to treat "foo[]" as "foo[0]" and foo[-1] as unsigned out-of-bounds.
          uniformBaseName = name.slice(0, leftBrace);
        }
  
        // Have we cached the location of this uniform before?
        var sizeAndId = program.uniformSizeAndIdsByName[uniformBaseName]; // A pair [array length, GLint of the uniform location]
  
        // If an uniform with this name exists, and if its index is within the array limits (if it's even an array),
        // query the WebGLlocation, or return an existing cached location.
        if (sizeAndId && arrayIndex < sizeAndId[0]) {
          arrayIndex += sizeAndId[1]; // Add the base location of the uniform to the array index offset.
          if ((uniformLocsById[arrayIndex] = uniformLocsById[arrayIndex] || GLctx.getUniformLocation(program, name))) {
            return arrayIndex;
          }
        }
      }
      else {
        // N.b. we are currently unable to distinguish between GL program IDs that never existed vs GL program IDs that have been deleted,
        // so report GL_INVALID_VALUE in both cases.
        GL.recordError(0x501 /* GL_INVALID_VALUE */);
      }
      return -1;
    }

  function webglGetUniformLocation(location) {
      var p = GLctx.currentProgram;
  
      if (p) {
        var webglLoc = p.uniformLocsById[location];
        // p.uniformLocsById[location] stores either an integer, or a WebGLUniformLocation.
  
        // If an integer, we have not yet bound the location, so do it now. The integer value specifies the array index
        // we should bind to.
        if (typeof webglLoc == 'number') {
          p.uniformLocsById[location] = webglLoc = GLctx.getUniformLocation(p, p.uniformArrayNamesById[location] + (webglLoc > 0 ? '[' + webglLoc + ']' : ''));
        }
        // Else an already cached WebGLUniformLocation, return it.
        return webglLoc;
      } else {
        GL.recordError(0x502/*GL_INVALID_OPERATION*/);
      }
    }
  /** @suppress{checkTypes} */
  function emscriptenWebGLGetUniform(program, location, params, type) {
      if (!params) {
        // GLES2 specification does not specify how to behave if params is a null pointer. Since calling this function does not make sense
        // if params == null, issue a GL error to notify user about it.
        GL.recordError(0x501 /* GL_INVALID_VALUE */);
        return;
      }
      program = GL.programs[program];
      webglPrepareUniformLocationsBeforeFirstUse(program);
      var data = GLctx.getUniform(program, webglGetUniformLocation(location));
      if (typeof data == 'number' || typeof data == 'boolean') {
        switch (type) {
          case 0: HEAP32[((params)>>2)] = data; break;
          case 2: HEAPF32[((params)>>2)] = data; break;
        }
      } else {
        for (var i = 0; i < data.length; i++) {
          switch (type) {
            case 0: HEAP32[(((params)+(i*4))>>2)] = data[i]; break;
            case 2: HEAPF32[(((params)+(i*4))>>2)] = data[i]; break;
          }
        }
      }
    }
  function _emscripten_glGetUniformfv(program, location, params) {
      emscriptenWebGLGetUniform(program, location, params, 2);
    }

  function _emscripten_glGetUniformiv(program, location, params) {
      emscriptenWebGLGetUniform(program, location, params, 0);
    }

  function _emscripten_glGetVertexAttribPointerv(index, pname, pointer) {
      if (!pointer) {
        // GLES2 specification does not specify how to behave if pointer is a null pointer. Since calling this function does not make sense
        // if pointer == null, issue a GL error to notify user about it.
        GL.recordError(0x501 /* GL_INVALID_VALUE */);
        return;
      }
      HEAP32[((pointer)>>2)] = GLctx.getVertexAttribOffset(index, pname);
    }

  /** @suppress{checkTypes} */
  function emscriptenWebGLGetVertexAttrib(index, pname, params, type) {
      if (!params) {
        // GLES2 specification does not specify how to behave if params is a null pointer. Since calling this function does not make sense
        // if params == null, issue a GL error to notify user about it.
        GL.recordError(0x501 /* GL_INVALID_VALUE */);
        return;
      }
      var data = GLctx.getVertexAttrib(index, pname);
      if (pname == 0x889F/*VERTEX_ATTRIB_ARRAY_BUFFER_BINDING*/) {
        HEAP32[((params)>>2)] = data && data["name"];
      } else if (typeof data == 'number' || typeof data == 'boolean') {
        switch (type) {
          case 0: HEAP32[((params)>>2)] = data; break;
          case 2: HEAPF32[((params)>>2)] = data; break;
          case 5: HEAP32[((params)>>2)] = Math.fround(data); break;
        }
      } else {
        for (var i = 0; i < data.length; i++) {
          switch (type) {
            case 0: HEAP32[(((params)+(i*4))>>2)] = data[i]; break;
            case 2: HEAPF32[(((params)+(i*4))>>2)] = data[i]; break;
            case 5: HEAP32[(((params)+(i*4))>>2)] = Math.fround(data[i]); break;
          }
        }
      }
    }
  function _emscripten_glGetVertexAttribfv(index, pname, params) {
      // N.B. This function may only be called if the vertex attribute was specified using the function glVertexAttrib*f(),
      // otherwise the results are undefined. (GLES3 spec 6.1.12)
      emscriptenWebGLGetVertexAttrib(index, pname, params, 2);
    }

  function _emscripten_glGetVertexAttribiv(index, pname, params) {
      // N.B. This function may only be called if the vertex attribute was specified using the function glVertexAttrib*f(),
      // otherwise the results are undefined. (GLES3 spec 6.1.12)
      emscriptenWebGLGetVertexAttrib(index, pname, params, 5);
    }

  function _emscripten_glHint(x0, x1) { GLctx['hint'](x0, x1) }

  function _emscripten_glIsBuffer(buffer) {
      var b = GL.buffers[buffer];
      if (!b) return 0;
      return GLctx.isBuffer(b);
    }

  function _emscripten_glIsEnabled(x0) { return GLctx['isEnabled'](x0) }

  function _emscripten_glIsFramebuffer(framebuffer) {
      var fb = GL.framebuffers[framebuffer];
      if (!fb) return 0;
      return GLctx.isFramebuffer(fb);
    }

  function _emscripten_glIsProgram(program) {
      program = GL.programs[program];
      if (!program) return 0;
      return GLctx.isProgram(program);
    }

  function _emscripten_glIsQueryEXT(id) {
      var query = GL.queries[id];
      if (!query) return 0;
      return GLctx.disjointTimerQueryExt['isQueryEXT'](query);
    }

  function _emscripten_glIsRenderbuffer(renderbuffer) {
      var rb = GL.renderbuffers[renderbuffer];
      if (!rb) return 0;
      return GLctx.isRenderbuffer(rb);
    }

  function _emscripten_glIsShader(shader) {
      var s = GL.shaders[shader];
      if (!s) return 0;
      return GLctx.isShader(s);
    }

  function _emscripten_glIsTexture(id) {
      var texture = GL.textures[id];
      if (!texture) return 0;
      return GLctx.isTexture(texture);
    }

  function _emscripten_glIsVertexArrayOES(array) {
  
      var vao = GL.vaos[array];
      if (!vao) return 0;
      return GLctx['isVertexArray'](vao);
    }

  function _emscripten_glLineWidth(x0) { GLctx['lineWidth'](x0) }

  function _emscripten_glLinkProgram(program) {
      program = GL.programs[program];
      GLctx.linkProgram(program);
      // Invalidate earlier computed uniform->ID mappings, those have now become stale
      program.uniformLocsById = 0; // Mark as null-like so that glGetUniformLocation() knows to populate this again.
      program.uniformSizeAndIdsByName = {};
  
    }

  function _emscripten_glPixelStorei(pname, param) {
      if (pname == 0xCF5 /* GL_UNPACK_ALIGNMENT */) {
        GL.unpackAlignment = param;
      }
      GLctx.pixelStorei(pname, param);
    }

  function _emscripten_glPolygonOffset(x0, x1) { GLctx['polygonOffset'](x0, x1) }

  function _emscripten_glQueryCounterEXT(id, target) {
      GLctx.disjointTimerQueryExt['queryCounterEXT'](GL.queries[id], target);
    }

  function computeUnpackAlignedImageSize(width, height, sizePerPixel, alignment) {
      function roundedToNextMultipleOf(x, y) {
        return (x + y - 1) & -y;
      }
      var plainRowSize = width * sizePerPixel;
      var alignedRowSize = roundedToNextMultipleOf(plainRowSize, alignment);
      return height * alignedRowSize;
    }
  
  function __colorChannelsInGlTextureFormat(format) {
      // Micro-optimizations for size: map format to size by subtracting smallest enum value (0x1902) from all values first.
      // Also omit the most common size value (1) from the list, which is assumed by formats not on the list.
      var colorChannels = {
        // 0x1902 /* GL_DEPTH_COMPONENT */ - 0x1902: 1,
        // 0x1906 /* GL_ALPHA */ - 0x1902: 1,
        5: 3,
        6: 4,
        // 0x1909 /* GL_LUMINANCE */ - 0x1902: 1,
        8: 2,
        29502: 3,
        29504: 4,
      };
      return colorChannels[format - 0x1902]||1;
    }
  
  function heapObjectForWebGLType(type) {
      // Micro-optimization for size: Subtract lowest GL enum number (0x1400/* GL_BYTE */) from type to compare
      // smaller values for the heap, for shorter generated code size.
      // Also the type HEAPU16 is not tested for explicitly, but any unrecognized type will return out HEAPU16.
      // (since most types are HEAPU16)
      type -= 0x1400;
  
      if (type == 1) return HEAPU8;
  
      if (type == 4) return HEAP32;
  
      if (type == 6) return HEAPF32;
  
      if (type == 5
        || type == 28922
        )
        return HEAPU32;
  
      return HEAPU16;
    }
  
  function heapAccessShiftForWebGLHeap(heap) {
      return 31 - Math.clz32(heap.BYTES_PER_ELEMENT);
    }
  function emscriptenWebGLGetTexPixelData(type, format, width, height, pixels, internalFormat) {
      var heap = heapObjectForWebGLType(type);
      var shift = heapAccessShiftForWebGLHeap(heap);
      var byteSize = 1<<shift;
      var sizePerPixel = __colorChannelsInGlTextureFormat(format) * byteSize;
      var bytes = computeUnpackAlignedImageSize(width, height, sizePerPixel, GL.unpackAlignment);
      return heap.subarray(pixels >> shift, pixels + bytes >> shift);
    }
  function _emscripten_glReadPixels(x, y, width, height, format, type, pixels) {
      var pixelData = emscriptenWebGLGetTexPixelData(type, format, width, height, pixels, format);
      if (!pixelData) {
        GL.recordError(0x500/*GL_INVALID_ENUM*/);
        return;
      }
      GLctx.readPixels(x, y, width, height, format, type, pixelData);
    }

  function _emscripten_glReleaseShaderCompiler() {
      // NOP (as allowed by GLES 2.0 spec)
    }

  function _emscripten_glRenderbufferStorage(x0, x1, x2, x3) { GLctx['renderbufferStorage'](x0, x1, x2, x3) }

  function _emscripten_glSampleCoverage(value, invert) {
      GLctx.sampleCoverage(value, !!invert);
    }

  function _emscripten_glScissor(x0, x1, x2, x3) { GLctx['scissor'](x0, x1, x2, x3) }

  function _emscripten_glShaderBinary() {
      GL.recordError(0x500/*GL_INVALID_ENUM*/);
    }

  function _emscripten_glShaderSource(shader, count, string, length) {
      var source = GL.getSource(shader, count, string, length);
  
      GLctx.shaderSource(GL.shaders[shader], source);
    }

  function _emscripten_glStencilFunc(x0, x1, x2) { GLctx['stencilFunc'](x0, x1, x2) }

  function _emscripten_glStencilFuncSeparate(x0, x1, x2, x3) { GLctx['stencilFuncSeparate'](x0, x1, x2, x3) }

  function _emscripten_glStencilMask(x0) { GLctx['stencilMask'](x0) }

  function _emscripten_glStencilMaskSeparate(x0, x1) { GLctx['stencilMaskSeparate'](x0, x1) }

  function _emscripten_glStencilOp(x0, x1, x2) { GLctx['stencilOp'](x0, x1, x2) }

  function _emscripten_glStencilOpSeparate(x0, x1, x2, x3) { GLctx['stencilOpSeparate'](x0, x1, x2, x3) }

  function _emscripten_glTexImage2D(target, level, internalFormat, width, height, border, format, type, pixels) {
      GLctx.texImage2D(target, level, internalFormat, width, height, border, format, type, pixels ? emscriptenWebGLGetTexPixelData(type, format, width, height, pixels, internalFormat) : null);
    }

  function _emscripten_glTexParameterf(x0, x1, x2) { GLctx['texParameterf'](x0, x1, x2) }

  function _emscripten_glTexParameterfv(target, pname, params) {
      var param = HEAPF32[((params)>>2)];
      GLctx.texParameterf(target, pname, param);
    }

  function _emscripten_glTexParameteri(x0, x1, x2) { GLctx['texParameteri'](x0, x1, x2) }

  function _emscripten_glTexParameteriv(target, pname, params) {
      var param = HEAP32[((params)>>2)];
      GLctx.texParameteri(target, pname, param);
    }

  function _emscripten_glTexSubImage2D(target, level, xoffset, yoffset, width, height, format, type, pixels) {
      var pixelData = null;
      if (pixels) pixelData = emscriptenWebGLGetTexPixelData(type, format, width, height, pixels, 0);
      GLctx.texSubImage2D(target, level, xoffset, yoffset, width, height, format, type, pixelData);
    }

  function _emscripten_glUniform1f(location, v0) {
      GLctx.uniform1f(webglGetUniformLocation(location), v0);
    }

  var miniTempWebGLFloatBuffers = [];
  function _emscripten_glUniform1fv(location, count, value) {
  
      if (count <= 288) {
        // avoid allocation when uploading few enough uniforms
        var view = miniTempWebGLFloatBuffers[count-1];
        for (var i = 0; i < count; ++i) {
          view[i] = HEAPF32[(((value)+(4*i))>>2)];
        }
      } else
      {
        var view = HEAPF32.subarray((value)>>2, (value+count*4)>>2);
      }
      GLctx.uniform1fv(webglGetUniformLocation(location), view);
    }

  function _emscripten_glUniform1i(location, v0) {
      GLctx.uniform1i(webglGetUniformLocation(location), v0);
    }

  var __miniTempWebGLIntBuffers = [];
  function _emscripten_glUniform1iv(location, count, value) {
  
      if (count <= 288) {
        // avoid allocation when uploading few enough uniforms
        var view = __miniTempWebGLIntBuffers[count-1];
        for (var i = 0; i < count; ++i) {
          view[i] = HEAP32[(((value)+(4*i))>>2)];
        }
      } else
      {
        var view = HEAP32.subarray((value)>>2, (value+count*4)>>2);
      }
      GLctx.uniform1iv(webglGetUniformLocation(location), view);
    }

  function _emscripten_glUniform2f(location, v0, v1) {
      GLctx.uniform2f(webglGetUniformLocation(location), v0, v1);
    }

  function _emscripten_glUniform2fv(location, count, value) {
  
      if (count <= 144) {
        // avoid allocation when uploading few enough uniforms
        var view = miniTempWebGLFloatBuffers[2*count-1];
        for (var i = 0; i < 2*count; i += 2) {
          view[i] = HEAPF32[(((value)+(4*i))>>2)];
          view[i+1] = HEAPF32[(((value)+(4*i+4))>>2)];
        }
      } else
      {
        var view = HEAPF32.subarray((value)>>2, (value+count*8)>>2);
      }
      GLctx.uniform2fv(webglGetUniformLocation(location), view);
    }

  function _emscripten_glUniform2i(location, v0, v1) {
      GLctx.uniform2i(webglGetUniformLocation(location), v0, v1);
    }

  function _emscripten_glUniform2iv(location, count, value) {
  
      if (count <= 144) {
        // avoid allocation when uploading few enough uniforms
        var view = __miniTempWebGLIntBuffers[2*count-1];
        for (var i = 0; i < 2*count; i += 2) {
          view[i] = HEAP32[(((value)+(4*i))>>2)];
          view[i+1] = HEAP32[(((value)+(4*i+4))>>2)];
        }
      } else
      {
        var view = HEAP32.subarray((value)>>2, (value+count*8)>>2);
      }
      GLctx.uniform2iv(webglGetUniformLocation(location), view);
    }

  function _emscripten_glUniform3f(location, v0, v1, v2) {
      GLctx.uniform3f(webglGetUniformLocation(location), v0, v1, v2);
    }

  function _emscripten_glUniform3fv(location, count, value) {
  
      if (count <= 96) {
        // avoid allocation when uploading few enough uniforms
        var view = miniTempWebGLFloatBuffers[3*count-1];
        for (var i = 0; i < 3*count; i += 3) {
          view[i] = HEAPF32[(((value)+(4*i))>>2)];
          view[i+1] = HEAPF32[(((value)+(4*i+4))>>2)];
          view[i+2] = HEAPF32[(((value)+(4*i+8))>>2)];
        }
      } else
      {
        var view = HEAPF32.subarray((value)>>2, (value+count*12)>>2);
      }
      GLctx.uniform3fv(webglGetUniformLocation(location), view);
    }

  function _emscripten_glUniform3i(location, v0, v1, v2) {
      GLctx.uniform3i(webglGetUniformLocation(location), v0, v1, v2);
    }

  function _emscripten_glUniform3iv(location, count, value) {
  
      if (count <= 96) {
        // avoid allocation when uploading few enough uniforms
        var view = __miniTempWebGLIntBuffers[3*count-1];
        for (var i = 0; i < 3*count; i += 3) {
          view[i] = HEAP32[(((value)+(4*i))>>2)];
          view[i+1] = HEAP32[(((value)+(4*i+4))>>2)];
          view[i+2] = HEAP32[(((value)+(4*i+8))>>2)];
        }
      } else
      {
        var view = HEAP32.subarray((value)>>2, (value+count*12)>>2);
      }
      GLctx.uniform3iv(webglGetUniformLocation(location), view);
    }

  function _emscripten_glUniform4f(location, v0, v1, v2, v3) {
      GLctx.uniform4f(webglGetUniformLocation(location), v0, v1, v2, v3);
    }

  function _emscripten_glUniform4fv(location, count, value) {
  
      if (count <= 72) {
        // avoid allocation when uploading few enough uniforms
        var view = miniTempWebGLFloatBuffers[4*count-1];
        // hoist the heap out of the loop for size and for pthreads+growth.
        var heap = HEAPF32;
        value >>= 2;
        for (var i = 0; i < 4 * count; i += 4) {
          var dst = value + i;
          view[i] = heap[dst];
          view[i + 1] = heap[dst + 1];
          view[i + 2] = heap[dst + 2];
          view[i + 3] = heap[dst + 3];
        }
      } else
      {
        var view = HEAPF32.subarray((value)>>2, (value+count*16)>>2);
      }
      GLctx.uniform4fv(webglGetUniformLocation(location), view);
    }

  function _emscripten_glUniform4i(location, v0, v1, v2, v3) {
      GLctx.uniform4i(webglGetUniformLocation(location), v0, v1, v2, v3);
    }

  function _emscripten_glUniform4iv(location, count, value) {
  
      if (count <= 72) {
        // avoid allocation when uploading few enough uniforms
        var view = __miniTempWebGLIntBuffers[4*count-1];
        for (var i = 0; i < 4*count; i += 4) {
          view[i] = HEAP32[(((value)+(4*i))>>2)];
          view[i+1] = HEAP32[(((value)+(4*i+4))>>2)];
          view[i+2] = HEAP32[(((value)+(4*i+8))>>2)];
          view[i+3] = HEAP32[(((value)+(4*i+12))>>2)];
        }
      } else
      {
        var view = HEAP32.subarray((value)>>2, (value+count*16)>>2);
      }
      GLctx.uniform4iv(webglGetUniformLocation(location), view);
    }

  function _emscripten_glUniformMatrix2fv(location, count, transpose, value) {
  
      if (count <= 72) {
        // avoid allocation when uploading few enough uniforms
        var view = miniTempWebGLFloatBuffers[4*count-1];
        for (var i = 0; i < 4*count; i += 4) {
          view[i] = HEAPF32[(((value)+(4*i))>>2)];
          view[i+1] = HEAPF32[(((value)+(4*i+4))>>2)];
          view[i+2] = HEAPF32[(((value)+(4*i+8))>>2)];
          view[i+3] = HEAPF32[(((value)+(4*i+12))>>2)];
        }
      } else
      {
        var view = HEAPF32.subarray((value)>>2, (value+count*16)>>2);
      }
      GLctx.uniformMatrix2fv(webglGetUniformLocation(location), !!transpose, view);
    }

  function _emscripten_glUniformMatrix3fv(location, count, transpose, value) {
  
      if (count <= 32) {
        // avoid allocation when uploading few enough uniforms
        var view = miniTempWebGLFloatBuffers[9*count-1];
        for (var i = 0; i < 9*count; i += 9) {
          view[i] = HEAPF32[(((value)+(4*i))>>2)];
          view[i+1] = HEAPF32[(((value)+(4*i+4))>>2)];
          view[i+2] = HEAPF32[(((value)+(4*i+8))>>2)];
          view[i+3] = HEAPF32[(((value)+(4*i+12))>>2)];
          view[i+4] = HEAPF32[(((value)+(4*i+16))>>2)];
          view[i+5] = HEAPF32[(((value)+(4*i+20))>>2)];
          view[i+6] = HEAPF32[(((value)+(4*i+24))>>2)];
          view[i+7] = HEAPF32[(((value)+(4*i+28))>>2)];
          view[i+8] = HEAPF32[(((value)+(4*i+32))>>2)];
        }
      } else
      {
        var view = HEAPF32.subarray((value)>>2, (value+count*36)>>2);
      }
      GLctx.uniformMatrix3fv(webglGetUniformLocation(location), !!transpose, view);
    }

  function _emscripten_glUniformMatrix4fv(location, count, transpose, value) {
  
      if (count <= 18) {
        // avoid allocation when uploading few enough uniforms
        var view = miniTempWebGLFloatBuffers[16*count-1];
        // hoist the heap out of the loop for size and for pthreads+growth.
        var heap = HEAPF32;
        value >>= 2;
        for (var i = 0; i < 16 * count; i += 16) {
          var dst = value + i;
          view[i] = heap[dst];
          view[i + 1] = heap[dst + 1];
          view[i + 2] = heap[dst + 2];
          view[i + 3] = heap[dst + 3];
          view[i + 4] = heap[dst + 4];
          view[i + 5] = heap[dst + 5];
          view[i + 6] = heap[dst + 6];
          view[i + 7] = heap[dst + 7];
          view[i + 8] = heap[dst + 8];
          view[i + 9] = heap[dst + 9];
          view[i + 10] = heap[dst + 10];
          view[i + 11] = heap[dst + 11];
          view[i + 12] = heap[dst + 12];
          view[i + 13] = heap[dst + 13];
          view[i + 14] = heap[dst + 14];
          view[i + 15] = heap[dst + 15];
        }
      } else
      {
        var view = HEAPF32.subarray((value)>>2, (value+count*64)>>2);
      }
      GLctx.uniformMatrix4fv(webglGetUniformLocation(location), !!transpose, view);
    }

  function _emscripten_glUseProgram(program) {
      program = GL.programs[program];
      GLctx.useProgram(program);
      // Record the currently active program so that we can access the uniform
      // mapping table of that program.
      GLctx.currentProgram = program;
    }

  function _emscripten_glValidateProgram(program) {
      GLctx.validateProgram(GL.programs[program]);
    }

  function _emscripten_glVertexAttrib1f(x0, x1) { GLctx['vertexAttrib1f'](x0, x1) }

  function _emscripten_glVertexAttrib1fv(index, v) {
  
      GLctx.vertexAttrib1f(index, HEAPF32[v>>2]);
    }

  function _emscripten_glVertexAttrib2f(x0, x1, x2) { GLctx['vertexAttrib2f'](x0, x1, x2) }

  function _emscripten_glVertexAttrib2fv(index, v) {
  
      GLctx.vertexAttrib2f(index, HEAPF32[v>>2], HEAPF32[v+4>>2]);
    }

  function _emscripten_glVertexAttrib3f(x0, x1, x2, x3) { GLctx['vertexAttrib3f'](x0, x1, x2, x3) }

  function _emscripten_glVertexAttrib3fv(index, v) {
  
      GLctx.vertexAttrib3f(index, HEAPF32[v>>2], HEAPF32[v+4>>2], HEAPF32[v+8>>2]);
    }

  function _emscripten_glVertexAttrib4f(x0, x1, x2, x3, x4) { GLctx['vertexAttrib4f'](x0, x1, x2, x3, x4) }

  function _emscripten_glVertexAttrib4fv(index, v) {
  
      GLctx.vertexAttrib4f(index, HEAPF32[v>>2], HEAPF32[v+4>>2], HEAPF32[v+8>>2], HEAPF32[v+12>>2]);
    }

  function _emscripten_glVertexAttribDivisorANGLE(index, divisor) {
      GLctx['vertexAttribDivisor'](index, divisor);
    }

  function _emscripten_glVertexAttribPointer(index, size, type, normalized, stride, ptr) {
      GLctx.vertexAttribPointer(index, size, type, !!normalized, stride, ptr);
    }

  function _emscripten_glViewport(x0, x1, x2, x3) { GLctx['viewport'](x0, x1, x2, x3) }

  function _emscripten_memcpy_big(dest, src, num) {
      HEAPU8.copyWithin(dest, src, src + num);
    }

  function _emscripten_request_pointerlock(target, deferUntilInEventHandler) {
      target = findEventTarget(target);
      if (!target) return -4;
      if (!target.requestPointerLock
        && !target.msRequestPointerLock
        ) {
        return -1;
      }
  
      var canPerformRequests = JSEvents.canPerformEventHandlerRequests();
  
      // Queue this function call if we're not currently in an event handler and the user saw it appropriate to do so.
      if (!canPerformRequests) {
        if (deferUntilInEventHandler) {
          JSEvents.deferCall(requestPointerLock, 2 /* priority below fullscreen */, [target]);
          return 1;
        } else {
          return -2;
        }
      }
  
      return requestPointerLock(target);
    }

  function emscripten_realloc_buffer(size) {
      try {
        // round size grow request up to wasm page size (fixed 64KB per spec)
        wasmMemory.grow((size - buffer.byteLength + 65535) >>> 16); // .grow() takes a delta compared to the previous size
        updateGlobalBufferAndViews(wasmMemory.buffer);
        return 1 /*success*/;
      } catch(e) {
      }
      // implicit 0 return to save code size (caller will cast "undefined" into 0
      // anyhow)
    }
  function _emscripten_resize_heap(requestedSize) {
      var oldSize = HEAPU8.length;
      requestedSize = requestedSize >>> 0;
      // With multithreaded builds, races can happen (another thread might increase the size
      // in between), so return a failure, and let the caller retry.
  
      // Memory resize rules:
      // 1.  Always increase heap size to at least the requested size, rounded up
      //     to next page multiple.
      // 2a. If MEMORY_GROWTH_LINEAR_STEP == -1, excessively resize the heap
      //     geometrically: increase the heap size according to
      //     MEMORY_GROWTH_GEOMETRIC_STEP factor (default +20%), At most
      //     overreserve by MEMORY_GROWTH_GEOMETRIC_CAP bytes (default 96MB).
      // 2b. If MEMORY_GROWTH_LINEAR_STEP != -1, excessively resize the heap
      //     linearly: increase the heap size by at least
      //     MEMORY_GROWTH_LINEAR_STEP bytes.
      // 3.  Max size for the heap is capped at 2048MB-WASM_PAGE_SIZE, or by
      //     MAXIMUM_MEMORY, or by ASAN limit, depending on which is smallest
      // 4.  If we were unable to allocate as much memory, it may be due to
      //     over-eager decision to excessively reserve due to (3) above.
      //     Hence if an allocation fails, cut down on the amount of excess
      //     growth, in an attempt to succeed to perform a smaller allocation.
  
      // A limit is set for how much we can grow. We should not exceed that
      // (the wasm binary specifies it, so if we tried, we'd fail anyhow).
      var maxHeapSize = getHeapMax();
      if (requestedSize > maxHeapSize) {
        return false;
      }
  
      let alignUp = (x, multiple) => x + (multiple - x % multiple) % multiple;
  
      // Loop through potential heap size increases. If we attempt a too eager
      // reservation that fails, cut down on the attempted size and reserve a
      // smaller bump instead. (max 3 times, chosen somewhat arbitrarily)
      for (var cutDown = 1; cutDown <= 4; cutDown *= 2) {
        var overGrownHeapSize = oldSize * (1 + 0.2 / cutDown); // ensure geometric growth
        // but limit overreserving (default to capping at +96MB overgrowth at most)
        overGrownHeapSize = Math.min(overGrownHeapSize, requestedSize + 100663296 );
  
        var newSize = Math.min(maxHeapSize, alignUp(Math.max(requestedSize, overGrownHeapSize), 65536));
  
        var replacement = emscripten_realloc_buffer(newSize);
        if (replacement) {
  
          return true;
        }
      }
      return false;
    }

  function _emscripten_run_script(ptr) {
      eval(UTF8ToString(ptr));
    }

  function _emscripten_sample_gamepad_data() {
      return (JSEvents.lastGamepadState = (navigator.getGamepads ? navigator.getGamepads() : (navigator.webkitGetGamepads ? navigator.webkitGetGamepads() : null)))
        ? 0 : -1;
    }

  function fillMouseEventData(eventStruct, e, target) {
      HEAPF64[((eventStruct)>>3)] = e.timeStamp;
      var idx = eventStruct >> 2;
      HEAP32[idx + 2] = e.screenX;
      HEAP32[idx + 3] = e.screenY;
      HEAP32[idx + 4] = e.clientX;
      HEAP32[idx + 5] = e.clientY;
      HEAP32[idx + 6] = e.ctrlKey;
      HEAP32[idx + 7] = e.shiftKey;
      HEAP32[idx + 8] = e.altKey;
      HEAP32[idx + 9] = e.metaKey;
      HEAP16[idx*2 + 20] = e.button;
      HEAP16[idx*2 + 21] = e.buttons;
  
      HEAP32[idx + 11] = e["movementX"]
        ;
  
      HEAP32[idx + 12] = e["movementY"]
        ;
  
      var rect = getBoundingClientRect(target);
      HEAP32[idx + 13] = e.clientX - rect.left;
      HEAP32[idx + 14] = e.clientY - rect.top;
  
    }
  function registerMouseEventCallback(target, userData, useCapture, callbackfunc, eventTypeId, eventTypeString, targetThread) {
      if (!JSEvents.mouseEvent) JSEvents.mouseEvent = _malloc( 72 );
      target = findEventTarget(target);
  
      var mouseEventHandlerFunc = function(ev) {
        var e = ev || event;
  
        // TODO: Make this access thread safe, or this could update live while app is reading it.
        fillMouseEventData(JSEvents.mouseEvent, e, target);
  
        if ((function(a1, a2, a3) { return dynCall_iiii.apply(null, [callbackfunc, a1, a2, a3]); })(eventTypeId, JSEvents.mouseEvent, userData)) e.preventDefault();
      };
  
      var eventHandler = {
        target: target,
        allowsDeferredCalls: eventTypeString != 'mousemove' && eventTypeString != 'mouseenter' && eventTypeString != 'mouseleave', // Mouse move events do not allow fullscreen/pointer lock requests to be handled in them!
        eventTypeString: eventTypeString,
        callbackfunc: callbackfunc,
        handlerFunc: mouseEventHandlerFunc,
        useCapture: useCapture
      };
      JSEvents.registerOrRemoveHandler(eventHandler);
    }
  function _emscripten_set_click_callback_on_thread(target, userData, useCapture, callbackfunc, targetThread) {
      registerMouseEventCallback(target, userData, useCapture, callbackfunc, 4, "click", targetThread);
      return 0;
    }

  function fillFullscreenChangeEventData(eventStruct) {
      var fullscreenElement = document.fullscreenElement || document.mozFullScreenElement || document.webkitFullscreenElement || document.msFullscreenElement;
      var isFullscreen = !!fullscreenElement;
      // Assigning a boolean to HEAP32 with expected type coercion.
      /** @suppress{checkTypes} */
      HEAP32[((eventStruct)>>2)] = isFullscreen;
      HEAP32[(((eventStruct)+(4))>>2)] = JSEvents.fullscreenEnabled();
      // If transitioning to fullscreen, report info about the element that is now fullscreen.
      // If transitioning to windowed mode, report info about the element that just was fullscreen.
      var reportedElement = isFullscreen ? fullscreenElement : JSEvents.previousFullscreenElement;
      var nodeName = JSEvents.getNodeNameForTarget(reportedElement);
      var id = (reportedElement && reportedElement.id) ? reportedElement.id : '';
      stringToUTF8(nodeName, eventStruct + 8, 128);
      stringToUTF8(id, eventStruct + 136, 128);
      HEAP32[(((eventStruct)+(264))>>2)] = reportedElement ? reportedElement.clientWidth : 0;
      HEAP32[(((eventStruct)+(268))>>2)] = reportedElement ? reportedElement.clientHeight : 0;
      HEAP32[(((eventStruct)+(272))>>2)] = screen.width;
      HEAP32[(((eventStruct)+(276))>>2)] = screen.height;
      if (isFullscreen) {
        JSEvents.previousFullscreenElement = fullscreenElement;
      }
    }
  function registerFullscreenChangeEventCallback(target, userData, useCapture, callbackfunc, eventTypeId, eventTypeString, targetThread) {
      if (!JSEvents.fullscreenChangeEvent) JSEvents.fullscreenChangeEvent = _malloc( 280 );
  
      var fullscreenChangeEventhandlerFunc = function(ev) {
        var e = ev || event;
  
        var fullscreenChangeEvent = JSEvents.fullscreenChangeEvent;
  
        fillFullscreenChangeEventData(fullscreenChangeEvent);
  
        if ((function(a1, a2, a3) { return dynCall_iiii.apply(null, [callbackfunc, a1, a2, a3]); })(eventTypeId, fullscreenChangeEvent, userData)) e.preventDefault();
      };
  
      var eventHandler = {
        target: target,
        eventTypeString: eventTypeString,
        callbackfunc: callbackfunc,
        handlerFunc: fullscreenChangeEventhandlerFunc,
        useCapture: useCapture
      };
      JSEvents.registerOrRemoveHandler(eventHandler);
    }
  function _emscripten_set_fullscreenchange_callback_on_thread(target, userData, useCapture, callbackfunc, targetThread) {
      if (!JSEvents.fullscreenEnabled()) return -1;
      target = findEventTarget(target);
      if (!target) return -4;
      registerFullscreenChangeEventCallback(target, userData, useCapture, callbackfunc, 19, "fullscreenchange", targetThread);
  
      // Unprefixed Fullscreen API shipped in Chromium 71 (https://bugs.chromium.org/p/chromium/issues/detail?id=383813)
      // As of Safari 13.0.3 on macOS Catalina 10.15.1 still ships with prefixed webkitfullscreenchange. TODO: revisit this check once Safari ships unprefixed version.
      registerFullscreenChangeEventCallback(target, userData, useCapture, callbackfunc, 19, "webkitfullscreenchange", targetThread);
  
      return 0;
    }

  function registerGamepadEventCallback(target, userData, useCapture, callbackfunc, eventTypeId, eventTypeString, targetThread) {
      if (!JSEvents.gamepadEvent) JSEvents.gamepadEvent = _malloc( 1432 );
  
      var gamepadEventHandlerFunc = function(ev) {
        var e = ev || event;
  
        var gamepadEvent = JSEvents.gamepadEvent;
        fillGamepadEventData(gamepadEvent, e["gamepad"]);
  
        if ((function(a1, a2, a3) { return dynCall_iiii.apply(null, [callbackfunc, a1, a2, a3]); })(eventTypeId, gamepadEvent, userData)) e.preventDefault();
      };
  
      var eventHandler = {
        target: findEventTarget(target),
        allowsDeferredCalls: true,
        eventTypeString: eventTypeString,
        callbackfunc: callbackfunc,
        handlerFunc: gamepadEventHandlerFunc,
        useCapture: useCapture
      };
      JSEvents.registerOrRemoveHandler(eventHandler);
    }
  function _emscripten_set_gamepadconnected_callback_on_thread(userData, useCapture, callbackfunc, targetThread) {
      if (!navigator.getGamepads && !navigator.webkitGetGamepads) return -1;
      registerGamepadEventCallback(2, userData, useCapture, callbackfunc, 26, "gamepadconnected", targetThread);
      return 0;
    }

  function _emscripten_set_gamepaddisconnected_callback_on_thread(userData, useCapture, callbackfunc, targetThread) {
      if (!navigator.getGamepads && !navigator.webkitGetGamepads) return -1;
      registerGamepadEventCallback(2, userData, useCapture, callbackfunc, 27, "gamepaddisconnected", targetThread);
      return 0;
    }

  function registerTouchEventCallback(target, userData, useCapture, callbackfunc, eventTypeId, eventTypeString, targetThread) {
      if (!JSEvents.touchEvent) JSEvents.touchEvent = _malloc( 1696 );
  
      target = findEventTarget(target);
  
      var touchEventHandlerFunc = function(e) {
        var t, touches = {}, et = e.touches;
        // To ease marshalling different kinds of touches that browser reports (all touches are listed in e.touches, 
        // only changed touches in e.changedTouches, and touches on target at a.targetTouches), mark a boolean in
        // each Touch object so that we can later loop only once over all touches we see to marshall over to Wasm.
  
        for (var i = 0; i < et.length; ++i) {
          t = et[i];
          // Browser might recycle the generated Touch objects between each frame (Firefox on Android), so reset any
          // changed/target states we may have set from previous frame.
          t.isChanged = t.onTarget = 0;
          touches[t.identifier] = t;
        }
        // Mark which touches are part of the changedTouches list.
        for (var i = 0; i < e.changedTouches.length; ++i) {
          t = e.changedTouches[i];
          t.isChanged = 1;
          touches[t.identifier] = t;
        }
        // Mark which touches are part of the targetTouches list.
        for (var i = 0; i < e.targetTouches.length; ++i) {
          touches[e.targetTouches[i].identifier].onTarget = 1;
        }
  
        var touchEvent = JSEvents.touchEvent;
        HEAPF64[((touchEvent)>>3)] = e.timeStamp;
        var idx = touchEvent>>2; // Pre-shift the ptr to index to HEAP32 to save code size
        HEAP32[idx + 3] = e.ctrlKey;
        HEAP32[idx + 4] = e.shiftKey;
        HEAP32[idx + 5] = e.altKey;
        HEAP32[idx + 6] = e.metaKey;
        idx += 7; // Advance to the start of the touch array.
        var targetRect = getBoundingClientRect(target);
        var numTouches = 0;
        for (var i in touches) {
          t = touches[i];
          HEAP32[idx + 0] = t.identifier;
          HEAP32[idx + 1] = t.screenX;
          HEAP32[idx + 2] = t.screenY;
          HEAP32[idx + 3] = t.clientX;
          HEAP32[idx + 4] = t.clientY;
          HEAP32[idx + 5] = t.pageX;
          HEAP32[idx + 6] = t.pageY;
          HEAP32[idx + 7] = t.isChanged;
          HEAP32[idx + 8] = t.onTarget;
          HEAP32[idx + 9] = t.clientX - targetRect.left;
          HEAP32[idx + 10] = t.clientY - targetRect.top;
  
          idx += 13;
  
          if (++numTouches > 31) {
            break;
          }
        }
        HEAP32[(((touchEvent)+(8))>>2)] = numTouches;
  
        if ((function(a1, a2, a3) { return dynCall_iiii.apply(null, [callbackfunc, a1, a2, a3]); })(eventTypeId, touchEvent, userData)) e.preventDefault();
      };
  
      var eventHandler = {
        target: target,
        allowsDeferredCalls: eventTypeString == 'touchstart' || eventTypeString == 'touchend',
        eventTypeString: eventTypeString,
        callbackfunc: callbackfunc,
        handlerFunc: touchEventHandlerFunc,
        useCapture: useCapture
      };
      JSEvents.registerOrRemoveHandler(eventHandler);
    }
  function _emscripten_set_touchcancel_callback_on_thread(target, userData, useCapture, callbackfunc, targetThread) {
      registerTouchEventCallback(target, userData, useCapture, callbackfunc, 25, "touchcancel", targetThread);
      return 0;
    }

  function _emscripten_set_touchend_callback_on_thread(target, userData, useCapture, callbackfunc, targetThread) {
      registerTouchEventCallback(target, userData, useCapture, callbackfunc, 23, "touchend", targetThread);
      return 0;
    }

  function _emscripten_set_touchmove_callback_on_thread(target, userData, useCapture, callbackfunc, targetThread) {
      registerTouchEventCallback(target, userData, useCapture, callbackfunc, 24, "touchmove", targetThread);
      return 0;
    }

  function _emscripten_set_touchstart_callback_on_thread(target, userData, useCapture, callbackfunc, targetThread) {
      registerTouchEventCallback(target, userData, useCapture, callbackfunc, 22, "touchstart", targetThread);
      return 0;
    }

  /** @param {boolean=} synchronous */
  function callUserCallback(func, synchronous) {
      if (ABORT) {
        return;
      }
      // For synchronous calls, let any exceptions propagate, and don't let the runtime exit.
      if (synchronous) {
        func();
        return;
      }
      try {
        func();
      } catch (e) {
        handleException(e);
      }
    }
  
  function runtimeKeepalivePush() {
    }
  
  function runtimeKeepalivePop() {
    }
  /** @param {number=} timeout */
  function safeSetTimeout(func, timeout) {
      
      return setTimeout(function() {
        
        callUserCallback(func);
      }, timeout);
    }
  function _emscripten_sleep(ms) {
      Asyncify.handleSleep((wakeUp) => safeSetTimeout(wakeUp, ms));
    }

  var ENV = {};
  
  function getExecutableName() {
      return thisProgram || './this.program';
    }
  function getEnvStrings() {
      if (!getEnvStrings.strings) {
        // Default values.
        // Browser language detection #8751
        var lang = ((typeof navigator == 'object' && navigator.languages && navigator.languages[0]) || 'C').replace('-', '_') + '.UTF-8';
        var env = {
          'USER': 'web_user',
          'LOGNAME': 'web_user',
          'PATH': '/',
          'PWD': '/',
          'HOME': '/home/web_user',
          'LANG': lang,
          '_': getExecutableName()
        };
        // Apply the user-provided values, if any.
        for (var x in ENV) {
          // x is a key in ENV; if ENV[x] is undefined, that means it was
          // explicitly set to be so. We allow user code to do that to
          // force variables with default values to remain unset.
          if (ENV[x] === undefined) delete env[x];
          else env[x] = ENV[x];
        }
        var strings = [];
        for (var x in env) {
          strings.push(x + '=' + env[x]);
        }
        getEnvStrings.strings = strings;
      }
      return getEnvStrings.strings;
    }
  function _environ_get(__environ, environ_buf) {
      var bufSize = 0;
      getEnvStrings().forEach(function(string, i) {
        var ptr = environ_buf + bufSize;
        HEAPU32[(((__environ)+(i*4))>>2)] = ptr;
        writeAsciiToMemory(string, ptr);
        bufSize += string.length + 1;
      });
      return 0;
    }

  function _environ_sizes_get(penviron_count, penviron_buf_size) {
      var strings = getEnvStrings();
      HEAPU32[((penviron_count)>>2)] = strings.length;
      var bufSize = 0;
      strings.forEach(function(string) {
        bufSize += string.length + 1;
      });
      HEAPU32[((penviron_buf_size)>>2)] = bufSize;
      return 0;
    }

  function _exit(status) {
      // void _exit(int status);
      // http://pubs.opengroup.org/onlinepubs/000095399/functions/exit.html
      exit(status);
    }

  function _fd_close(fd) {
  try {
  
      var stream = SYSCALLS.getStreamFromFD(fd);
      FS.close(stream);
      return 0;
    } catch (e) {
    if (typeof FS == 'undefined' || !(e instanceof FS.ErrnoError)) throw e;
    return e.errno;
  }
  }

  /** @param {number=} offset */
  function doReadv(stream, iov, iovcnt, offset) {
      var ret = 0;
      for (var i = 0; i < iovcnt; i++) {
        var ptr = HEAPU32[((iov)>>2)];
        var len = HEAPU32[(((iov)+(4))>>2)];
        iov += 8;
        var curr = FS.read(stream, HEAP8,ptr, len, offset);
        if (curr < 0) return -1;
        ret += curr;
        if (curr < len) break; // nothing more to read
      }
      return ret;
    }
  function _fd_pread(fd, iov, iovcnt, offset_low, offset_high, pnum) {
  try {
  
      var offset = convertI32PairToI53Checked(offset_low, offset_high); if (isNaN(offset)) return 61;
      var stream = SYSCALLS.getStreamFromFD(fd)
      var num = doReadv(stream, iov, iovcnt, offset);
      HEAP32[((pnum)>>2)] = num;
      return 0;
    } catch (e) {
    if (typeof FS == 'undefined' || !(e instanceof FS.ErrnoError)) throw e;
    return e.errno;
  }
  }

  /** @param {number=} offset */
  function doWritev(stream, iov, iovcnt, offset) {
      var ret = 0;
      for (var i = 0; i < iovcnt; i++) {
        var ptr = HEAPU32[((iov)>>2)];
        var len = HEAPU32[(((iov)+(4))>>2)];
        iov += 8;
        var curr = FS.write(stream, HEAP8,ptr, len, offset);
        if (curr < 0) return -1;
        ret += curr;
      }
      return ret;
    }
  function _fd_pwrite(fd, iov, iovcnt, offset_low, offset_high, pnum) {
  try {
  
      var offset = convertI32PairToI53Checked(offset_low, offset_high); if (isNaN(offset)) return 61;
      var stream = SYSCALLS.getStreamFromFD(fd)
      var num = doWritev(stream, iov, iovcnt, offset);
      HEAP32[((pnum)>>2)] = num;
      return 0;
    } catch (e) {
    if (typeof FS == 'undefined' || !(e instanceof FS.ErrnoError)) throw e;
    return e.errno;
  }
  }

  function _fd_read(fd, iov, iovcnt, pnum) {
  try {
  
      var stream = SYSCALLS.getStreamFromFD(fd);
      var num = doReadv(stream, iov, iovcnt);
      HEAP32[((pnum)>>2)] = num;
      return 0;
    } catch (e) {
    if (typeof FS == 'undefined' || !(e instanceof FS.ErrnoError)) throw e;
    return e.errno;
  }
  }

  function _fd_seek(fd, offset_low, offset_high, whence, newOffset) {
  try {
  
      var offset = convertI32PairToI53Checked(offset_low, offset_high); if (isNaN(offset)) return 61;
      var stream = SYSCALLS.getStreamFromFD(fd);
      FS.llseek(stream, offset, whence);
      (tempI64 = [stream.position>>>0,(tempDouble=stream.position,(+(Math.abs(tempDouble))) >= 1.0 ? (tempDouble > 0.0 ? ((Math.min((+(Math.floor((tempDouble)/4294967296.0))), 4294967295.0))|0)>>>0 : (~~((+(Math.ceil((tempDouble - +(((~~(tempDouble)))>>>0))/4294967296.0)))))>>>0) : 0)],HEAP32[((newOffset)>>2)] = tempI64[0],HEAP32[(((newOffset)+(4))>>2)] = tempI64[1]);
      if (stream.getdents && offset === 0 && whence === 0) stream.getdents = null; // reset readdir state
      return 0;
    } catch (e) {
    if (typeof FS == 'undefined' || !(e instanceof FS.ErrnoError)) throw e;
    return e.errno;
  }
  }

  function _fd_write(fd, iov, iovcnt, pnum) {
  try {
  
      var stream = SYSCALLS.getStreamFromFD(fd);
      var num = doWritev(stream, iov, iovcnt);
      HEAPU32[((pnum)>>2)] = num;
      return 0;
    } catch (e) {
    if (typeof FS == 'undefined' || !(e instanceof FS.ErrnoError)) throw e;
    return e.errno;
  }
  }

  function _getTempRet0() {
      return getTempRet0();
    }

  function _glActiveTexture(x0) { GLctx['activeTexture'](x0) }

  function _glAttachShader(program, shader) {
      GLctx.attachShader(GL.programs[program], GL.shaders[shader]);
    }

  function _glBindAttribLocation(program, index, name) {
      GLctx.bindAttribLocation(GL.programs[program], index, UTF8ToString(name));
    }

  function _glBindBuffer(target, buffer) {
  
      GLctx.bindBuffer(target, GL.buffers[buffer]);
    }

  function _glBindTexture(target, texture) {
      GLctx.bindTexture(target, GL.textures[texture]);
    }

  function _glBlendFunc(x0, x1) { GLctx['blendFunc'](x0, x1) }

  function _glBufferData(target, size, data, usage) {
  
        // N.b. here first form specifies a heap subarray, second form an integer size, so the ?: code here is polymorphic. It is advised to avoid
        // randomly mixing both uses in calling code, to avoid any potential JS engine JIT issues.
        GLctx.bufferData(target, data ? HEAPU8.subarray(data, data+size) : size, usage);
    }

  function _glBufferSubData(target, offset, size, data) {
      GLctx.bufferSubData(target, offset, HEAPU8.subarray(data, data+size));
    }

  function _glClear(x0) { GLctx['clear'](x0) }

  function _glClearColor(x0, x1, x2, x3) { GLctx['clearColor'](x0, x1, x2, x3) }

  function _glClearDepthf(x0) { GLctx['clearDepth'](x0) }

  function _glCompileShader(shader) {
      GLctx.compileShader(GL.shaders[shader]);
    }

  function _glCompressedTexImage2D(target, level, internalFormat, width, height, border, imageSize, data) {
      GLctx['compressedTexImage2D'](target, level, internalFormat, width, height, border, data ? HEAPU8.subarray((data), (data+imageSize)) : null);
    }

  function _glCreateProgram() {
      var id = GL.getNewId(GL.programs);
      var program = GLctx.createProgram();
      // Store additional information needed for each shader program:
      program.name = id;
      // Lazy cache results of glGetProgramiv(GL_ACTIVE_UNIFORM_MAX_LENGTH/GL_ACTIVE_ATTRIBUTE_MAX_LENGTH/GL_ACTIVE_UNIFORM_BLOCK_MAX_NAME_LENGTH)
      program.maxUniformLength = program.maxAttributeLength = program.maxUniformBlockNameLength = 0;
      program.uniformIdCounter = 1;
      GL.programs[id] = program;
      return id;
    }

  function _glCreateShader(shaderType) {
      var id = GL.getNewId(GL.shaders);
      GL.shaders[id] = GLctx.createShader(shaderType);
  
      return id;
    }

  function _glCullFace(x0) { GLctx['cullFace'](x0) }

  function _glDeleteBuffers(n, buffers) {
      for (var i = 0; i < n; i++) {
        var id = HEAP32[(((buffers)+(i*4))>>2)];
        var buffer = GL.buffers[id];
  
        // From spec: "glDeleteBuffers silently ignores 0's and names that do not
        // correspond to existing buffer objects."
        if (!buffer) continue;
  
        GLctx.deleteBuffer(buffer);
        buffer.name = 0;
        GL.buffers[id] = null;
  
      }
    }

  function _glDeleteProgram(id) {
      if (!id) return;
      var program = GL.programs[id];
      if (!program) { // glDeleteProgram actually signals an error when deleting a nonexisting object, unlike some other GL delete functions.
        GL.recordError(0x501 /* GL_INVALID_VALUE */);
        return;
      }
      GLctx.deleteProgram(program);
      program.name = 0;
      GL.programs[id] = null;
    }

  function _glDeleteShader(id) {
      if (!id) return;
      var shader = GL.shaders[id];
      if (!shader) { // glDeleteShader actually signals an error when deleting a nonexisting object, unlike some other GL delete functions.
        GL.recordError(0x501 /* GL_INVALID_VALUE */);
        return;
      }
      GLctx.deleteShader(shader);
      GL.shaders[id] = null;
    }

  function _glDeleteTextures(n, textures) {
      for (var i = 0; i < n; i++) {
        var id = HEAP32[(((textures)+(i*4))>>2)];
        var texture = GL.textures[id];
        if (!texture) continue; // GL spec: "glDeleteTextures silently ignores 0s and names that do not correspond to existing textures".
        GLctx.deleteTexture(texture);
        texture.name = 0;
        GL.textures[id] = null;
      }
    }

  function _glDepthFunc(x0) { GLctx['depthFunc'](x0) }

  function _glDetachShader(program, shader) {
      GLctx.detachShader(GL.programs[program], GL.shaders[shader]);
    }

  function _glDisable(x0) { GLctx['disable'](x0) }

  function _glDisableVertexAttribArray(index) {
      GLctx.disableVertexAttribArray(index);
    }

  function _glDrawArrays(mode, first, count) {
  
      GLctx.drawArrays(mode, first, count);
  
    }

  function _glDrawElements(mode, count, type, indices) {
  
      GLctx.drawElements(mode, count, type, indices);
  
    }

  function _glEnable(x0) { GLctx['enable'](x0) }

  function _glEnableVertexAttribArray(index) {
      GLctx.enableVertexAttribArray(index);
    }

  function _glFrontFace(x0) { GLctx['frontFace'](x0) }

  function _glGenBuffers(n, buffers) {
      __glGenObject(n, buffers, 'createBuffer', GL.buffers
        );
    }

  function _glGenTextures(n, textures) {
      __glGenObject(n, textures, 'createTexture', GL.textures
        );
    }

  function _glGetAttribLocation(program, name) {
      return GLctx.getAttribLocation(GL.programs[program], UTF8ToString(name));
    }

  function _glGetFloatv(name_, p) {
      emscriptenWebGLGet(name_, p, 2);
    }

  function _glGetProgramInfoLog(program, maxLength, length, infoLog) {
      var log = GLctx.getProgramInfoLog(GL.programs[program]);
      if (log === null) log = '(unknown error)';
      var numBytesWrittenExclNull = (maxLength > 0 && infoLog) ? stringToUTF8(log, infoLog, maxLength) : 0;
      if (length) HEAP32[((length)>>2)] = numBytesWrittenExclNull;
    }

  function _glGetProgramiv(program, pname, p) {
      if (!p) {
        // GLES2 specification does not specify how to behave if p is a null pointer. Since calling this function does not make sense
        // if p == null, issue a GL error to notify user about it.
        GL.recordError(0x501 /* GL_INVALID_VALUE */);
        return;
      }
  
      if (program >= GL.counter) {
        GL.recordError(0x501 /* GL_INVALID_VALUE */);
        return;
      }
  
      program = GL.programs[program];
  
      if (pname == 0x8B84) { // GL_INFO_LOG_LENGTH
        var log = GLctx.getProgramInfoLog(program);
        if (log === null) log = '(unknown error)';
        HEAP32[((p)>>2)] = log.length + 1;
      } else if (pname == 0x8B87 /* GL_ACTIVE_UNIFORM_MAX_LENGTH */) {
        if (!program.maxUniformLength) {
          for (var i = 0; i < GLctx.getProgramParameter(program, 0x8B86/*GL_ACTIVE_UNIFORMS*/); ++i) {
            program.maxUniformLength = Math.max(program.maxUniformLength, GLctx.getActiveUniform(program, i).name.length+1);
          }
        }
        HEAP32[((p)>>2)] = program.maxUniformLength;
      } else if (pname == 0x8B8A /* GL_ACTIVE_ATTRIBUTE_MAX_LENGTH */) {
        if (!program.maxAttributeLength) {
          for (var i = 0; i < GLctx.getProgramParameter(program, 0x8B89/*GL_ACTIVE_ATTRIBUTES*/); ++i) {
            program.maxAttributeLength = Math.max(program.maxAttributeLength, GLctx.getActiveAttrib(program, i).name.length+1);
          }
        }
        HEAP32[((p)>>2)] = program.maxAttributeLength;
      } else if (pname == 0x8A35 /* GL_ACTIVE_UNIFORM_BLOCK_MAX_NAME_LENGTH */) {
        if (!program.maxUniformBlockNameLength) {
          for (var i = 0; i < GLctx.getProgramParameter(program, 0x8A36/*GL_ACTIVE_UNIFORM_BLOCKS*/); ++i) {
            program.maxUniformBlockNameLength = Math.max(program.maxUniformBlockNameLength, GLctx.getActiveUniformBlockName(program, i).length+1);
          }
        }
        HEAP32[((p)>>2)] = program.maxUniformBlockNameLength;
      } else {
        HEAP32[((p)>>2)] = GLctx.getProgramParameter(program, pname);
      }
    }

  function _glGetShaderInfoLog(shader, maxLength, length, infoLog) {
      var log = GLctx.getShaderInfoLog(GL.shaders[shader]);
      if (log === null) log = '(unknown error)';
      var numBytesWrittenExclNull = (maxLength > 0 && infoLog) ? stringToUTF8(log, infoLog, maxLength) : 0;
      if (length) HEAP32[((length)>>2)] = numBytesWrittenExclNull;
    }

  function _glGetShaderiv(shader, pname, p) {
      if (!p) {
        // GLES2 specification does not specify how to behave if p is a null pointer. Since calling this function does not make sense
        // if p == null, issue a GL error to notify user about it.
        GL.recordError(0x501 /* GL_INVALID_VALUE */);
        return;
      }
      if (pname == 0x8B84) { // GL_INFO_LOG_LENGTH
        var log = GLctx.getShaderInfoLog(GL.shaders[shader]);
        if (log === null) log = '(unknown error)';
        // The GLES2 specification says that if the shader has an empty info log,
        // a value of 0 is returned. Otherwise the log has a null char appended.
        // (An empty string is falsey, so we can just check that instead of
        // looking at log.length.)
        var logLength = log ? log.length + 1 : 0;
        HEAP32[((p)>>2)] = logLength;
      } else if (pname == 0x8B88) { // GL_SHADER_SOURCE_LENGTH
        var source = GLctx.getShaderSource(GL.shaders[shader]);
        // source may be a null, or the empty string, both of which are falsey
        // values that we report a 0 length for.
        var sourceLength = source ? source.length + 1 : 0;
        HEAP32[((p)>>2)] = sourceLength;
      } else {
        HEAP32[((p)>>2)] = GLctx.getShaderParameter(GL.shaders[shader], pname);
      }
    }

  function _glGetString(name_) {
      var ret = GL.stringCache[name_];
      if (!ret) {
        switch (name_) {
          case 0x1F03 /* GL_EXTENSIONS */:
            var exts = GLctx.getSupportedExtensions() || []; // .getSupportedExtensions() can return null if context is lost, so coerce to empty array.
            exts = exts.concat(exts.map(function(e) { return "GL_" + e; }));
            ret = stringToNewUTF8(exts.join(' '));
            break;
          case 0x1F00 /* GL_VENDOR */:
          case 0x1F01 /* GL_RENDERER */:
          case 0x9245 /* UNMASKED_VENDOR_WEBGL */:
          case 0x9246 /* UNMASKED_RENDERER_WEBGL */:
            var s = GLctx.getParameter(name_);
            if (!s) {
              GL.recordError(0x500/*GL_INVALID_ENUM*/);
            }
            ret = s && stringToNewUTF8(s);
            break;
  
          case 0x1F02 /* GL_VERSION */:
            var glVersion = GLctx.getParameter(0x1F02 /*GL_VERSION*/);
            // return GLES version string corresponding to the version of the WebGL context
            {
              glVersion = 'OpenGL ES 2.0 (' + glVersion + ')';
            }
            ret = stringToNewUTF8(glVersion);
            break;
          case 0x8B8C /* GL_SHADING_LANGUAGE_VERSION */:
            var glslVersion = GLctx.getParameter(0x8B8C /*GL_SHADING_LANGUAGE_VERSION*/);
            // extract the version number 'N.M' from the string 'WebGL GLSL ES N.M ...'
            var ver_re = /^WebGL GLSL ES ([0-9]\.[0-9][0-9]?)(?:$| .*)/;
            var ver_num = glslVersion.match(ver_re);
            if (ver_num !== null) {
              if (ver_num[1].length == 3) ver_num[1] = ver_num[1] + '0'; // ensure minor version has 2 digits
              glslVersion = 'OpenGL ES GLSL ES ' + ver_num[1] + ' (' + glslVersion + ')';
            }
            ret = stringToNewUTF8(glslVersion);
            break;
          default:
            GL.recordError(0x500/*GL_INVALID_ENUM*/);
            // fall through
        }
        GL.stringCache[name_] = ret;
      }
      return ret;
    }

  function _glGetUniformLocation(program, name) {
  
      name = UTF8ToString(name);
  
      if (program = GL.programs[program]) {
        webglPrepareUniformLocationsBeforeFirstUse(program);
        var uniformLocsById = program.uniformLocsById; // Maps GLuint -> WebGLUniformLocation
        var arrayIndex = 0;
        var uniformBaseName = name;
  
        // Invariant: when populating integer IDs for uniform locations, we must maintain the precondition that
        // arrays reside in contiguous addresses, i.e. for a 'vec4 colors[10];', colors[4] must be at location colors[0]+4.
        // However, user might call glGetUniformLocation(program, "colors") for an array, so we cannot discover based on the user
        // input arguments whether the uniform we are dealing with is an array. The only way to discover which uniforms are arrays
        // is to enumerate over all the active uniforms in the program.
        var leftBrace = webglGetLeftBracePos(name);
  
        // If user passed an array accessor "[index]", parse the array index off the accessor.
        if (leftBrace > 0) {
          arrayIndex = jstoi_q(name.slice(leftBrace + 1)) >>> 0; // "index]", coerce parseInt(']') with >>>0 to treat "foo[]" as "foo[0]" and foo[-1] as unsigned out-of-bounds.
          uniformBaseName = name.slice(0, leftBrace);
        }
  
        // Have we cached the location of this uniform before?
        var sizeAndId = program.uniformSizeAndIdsByName[uniformBaseName]; // A pair [array length, GLint of the uniform location]
  
        // If an uniform with this name exists, and if its index is within the array limits (if it's even an array),
        // query the WebGLlocation, or return an existing cached location.
        if (sizeAndId && arrayIndex < sizeAndId[0]) {
          arrayIndex += sizeAndId[1]; // Add the base location of the uniform to the array index offset.
          if ((uniformLocsById[arrayIndex] = uniformLocsById[arrayIndex] || GLctx.getUniformLocation(program, name))) {
            return arrayIndex;
          }
        }
      }
      else {
        // N.b. we are currently unable to distinguish between GL program IDs that never existed vs GL program IDs that have been deleted,
        // so report GL_INVALID_VALUE in both cases.
        GL.recordError(0x501 /* GL_INVALID_VALUE */);
      }
      return -1;
    }

  function _glLinkProgram(program) {
      program = GL.programs[program];
      GLctx.linkProgram(program);
      // Invalidate earlier computed uniform->ID mappings, those have now become stale
      program.uniformLocsById = 0; // Mark as null-like so that glGetUniformLocation() knows to populate this again.
      program.uniformSizeAndIdsByName = {};
  
    }

  function _glPixelStorei(pname, param) {
      if (pname == 0xCF5 /* GL_UNPACK_ALIGNMENT */) {
        GL.unpackAlignment = param;
      }
      GLctx.pixelStorei(pname, param);
    }

  function _glReadPixels(x, y, width, height, format, type, pixels) {
      var pixelData = emscriptenWebGLGetTexPixelData(type, format, width, height, pixels, format);
      if (!pixelData) {
        GL.recordError(0x500/*GL_INVALID_ENUM*/);
        return;
      }
      GLctx.readPixels(x, y, width, height, format, type, pixelData);
    }

  function _glShaderSource(shader, count, string, length) {
      var source = GL.getSource(shader, count, string, length);
  
      GLctx.shaderSource(GL.shaders[shader], source);
    }

  function _glTexImage2D(target, level, internalFormat, width, height, border, format, type, pixels) {
      GLctx.texImage2D(target, level, internalFormat, width, height, border, format, type, pixels ? emscriptenWebGLGetTexPixelData(type, format, width, height, pixels, internalFormat) : null);
    }

  function _glTexParameterf(x0, x1, x2) { GLctx['texParameterf'](x0, x1, x2) }

  function _glTexParameteri(x0, x1, x2) { GLctx['texParameteri'](x0, x1, x2) }

  function _glUniform1fv(location, count, value) {
  
      if (count <= 288) {
        // avoid allocation when uploading few enough uniforms
        var view = miniTempWebGLFloatBuffers[count-1];
        for (var i = 0; i < count; ++i) {
          view[i] = HEAPF32[(((value)+(4*i))>>2)];
        }
      } else
      {
        var view = HEAPF32.subarray((value)>>2, (value+count*4)>>2);
      }
      GLctx.uniform1fv(webglGetUniformLocation(location), view);
    }

  function _glUniform1i(location, v0) {
      GLctx.uniform1i(webglGetUniformLocation(location), v0);
    }

  function _glUniform1iv(location, count, value) {
  
      if (count <= 288) {
        // avoid allocation when uploading few enough uniforms
        var view = __miniTempWebGLIntBuffers[count-1];
        for (var i = 0; i < count; ++i) {
          view[i] = HEAP32[(((value)+(4*i))>>2)];
        }
      } else
      {
        var view = HEAP32.subarray((value)>>2, (value+count*4)>>2);
      }
      GLctx.uniform1iv(webglGetUniformLocation(location), view);
    }

  function _glUniform2fv(location, count, value) {
  
      if (count <= 144) {
        // avoid allocation when uploading few enough uniforms
        var view = miniTempWebGLFloatBuffers[2*count-1];
        for (var i = 0; i < 2*count; i += 2) {
          view[i] = HEAPF32[(((value)+(4*i))>>2)];
          view[i+1] = HEAPF32[(((value)+(4*i+4))>>2)];
        }
      } else
      {
        var view = HEAPF32.subarray((value)>>2, (value+count*8)>>2);
      }
      GLctx.uniform2fv(webglGetUniformLocation(location), view);
    }

  function _glUniform2iv(location, count, value) {
  
      if (count <= 144) {
        // avoid allocation when uploading few enough uniforms
        var view = __miniTempWebGLIntBuffers[2*count-1];
        for (var i = 0; i < 2*count; i += 2) {
          view[i] = HEAP32[(((value)+(4*i))>>2)];
          view[i+1] = HEAP32[(((value)+(4*i+4))>>2)];
        }
      } else
      {
        var view = HEAP32.subarray((value)>>2, (value+count*8)>>2);
      }
      GLctx.uniform2iv(webglGetUniformLocation(location), view);
    }

  function _glUniform3fv(location, count, value) {
  
      if (count <= 96) {
        // avoid allocation when uploading few enough uniforms
        var view = miniTempWebGLFloatBuffers[3*count-1];
        for (var i = 0; i < 3*count; i += 3) {
          view[i] = HEAPF32[(((value)+(4*i))>>2)];
          view[i+1] = HEAPF32[(((value)+(4*i+4))>>2)];
          view[i+2] = HEAPF32[(((value)+(4*i+8))>>2)];
        }
      } else
      {
        var view = HEAPF32.subarray((value)>>2, (value+count*12)>>2);
      }
      GLctx.uniform3fv(webglGetUniformLocation(location), view);
    }

  function _glUniform3iv(location, count, value) {
  
      if (count <= 96) {
        // avoid allocation when uploading few enough uniforms
        var view = __miniTempWebGLIntBuffers[3*count-1];
        for (var i = 0; i < 3*count; i += 3) {
          view[i] = HEAP32[(((value)+(4*i))>>2)];
          view[i+1] = HEAP32[(((value)+(4*i+4))>>2)];
          view[i+2] = HEAP32[(((value)+(4*i+8))>>2)];
        }
      } else
      {
        var view = HEAP32.subarray((value)>>2, (value+count*12)>>2);
      }
      GLctx.uniform3iv(webglGetUniformLocation(location), view);
    }

  function _glUniform4f(location, v0, v1, v2, v3) {
      GLctx.uniform4f(webglGetUniformLocation(location), v0, v1, v2, v3);
    }

  function _glUniform4fv(location, count, value) {
  
      if (count <= 72) {
        // avoid allocation when uploading few enough uniforms
        var view = miniTempWebGLFloatBuffers[4*count-1];
        // hoist the heap out of the loop for size and for pthreads+growth.
        var heap = HEAPF32;
        value >>= 2;
        for (var i = 0; i < 4 * count; i += 4) {
          var dst = value + i;
          view[i] = heap[dst];
          view[i + 1] = heap[dst + 1];
          view[i + 2] = heap[dst + 2];
          view[i + 3] = heap[dst + 3];
        }
      } else
      {
        var view = HEAPF32.subarray((value)>>2, (value+count*16)>>2);
      }
      GLctx.uniform4fv(webglGetUniformLocation(location), view);
    }

  function _glUniform4iv(location, count, value) {
  
      if (count <= 72) {
        // avoid allocation when uploading few enough uniforms
        var view = __miniTempWebGLIntBuffers[4*count-1];
        for (var i = 0; i < 4*count; i += 4) {
          view[i] = HEAP32[(((value)+(4*i))>>2)];
          view[i+1] = HEAP32[(((value)+(4*i+4))>>2)];
          view[i+2] = HEAP32[(((value)+(4*i+8))>>2)];
          view[i+3] = HEAP32[(((value)+(4*i+12))>>2)];
        }
      } else
      {
        var view = HEAP32.subarray((value)>>2, (value+count*16)>>2);
      }
      GLctx.uniform4iv(webglGetUniformLocation(location), view);
    }

  function _glUniformMatrix4fv(location, count, transpose, value) {
  
      if (count <= 18) {
        // avoid allocation when uploading few enough uniforms
        var view = miniTempWebGLFloatBuffers[16*count-1];
        // hoist the heap out of the loop for size and for pthreads+growth.
        var heap = HEAPF32;
        value >>= 2;
        for (var i = 0; i < 16 * count; i += 16) {
          var dst = value + i;
          view[i] = heap[dst];
          view[i + 1] = heap[dst + 1];
          view[i + 2] = heap[dst + 2];
          view[i + 3] = heap[dst + 3];
          view[i + 4] = heap[dst + 4];
          view[i + 5] = heap[dst + 5];
          view[i + 6] = heap[dst + 6];
          view[i + 7] = heap[dst + 7];
          view[i + 8] = heap[dst + 8];
          view[i + 9] = heap[dst + 9];
          view[i + 10] = heap[dst + 10];
          view[i + 11] = heap[dst + 11];
          view[i + 12] = heap[dst + 12];
          view[i + 13] = heap[dst + 13];
          view[i + 14] = heap[dst + 14];
          view[i + 15] = heap[dst + 15];
        }
      } else
      {
        var view = HEAPF32.subarray((value)>>2, (value+count*64)>>2);
      }
      GLctx.uniformMatrix4fv(webglGetUniformLocation(location), !!transpose, view);
    }

  function _glUseProgram(program) {
      program = GL.programs[program];
      GLctx.useProgram(program);
      // Record the currently active program so that we can access the uniform
      // mapping table of that program.
      GLctx.currentProgram = program;
    }

  function _glVertexAttrib1fv(index, v) {
  
      GLctx.vertexAttrib1f(index, HEAPF32[v>>2]);
    }

  function _glVertexAttrib2fv(index, v) {
  
      GLctx.vertexAttrib2f(index, HEAPF32[v>>2], HEAPF32[v+4>>2]);
    }

  function _glVertexAttrib3fv(index, v) {
  
      GLctx.vertexAttrib3f(index, HEAPF32[v>>2], HEAPF32[v+4>>2], HEAPF32[v+8>>2]);
    }

  function _glVertexAttrib4fv(index, v) {
  
      GLctx.vertexAttrib4f(index, HEAPF32[v>>2], HEAPF32[v+4>>2], HEAPF32[v+8>>2], HEAPF32[v+12>>2]);
    }

  function _glVertexAttribPointer(index, size, type, normalized, stride, ptr) {
      GLctx.vertexAttribPointer(index, size, type, !!normalized, stride, ptr);
    }

  function _glViewport(x0, x1, x2, x3) { GLctx['viewport'](x0, x1, x2, x3) }

  function _emscripten_set_main_loop_timing(mode, value) {
      Browser.mainLoop.timingMode = mode;
      Browser.mainLoop.timingValue = value;
  
      if (!Browser.mainLoop.func) {
        return 1; // Return non-zero on failure, can't set timing mode when there is no main loop.
      }
  
      if (!Browser.mainLoop.running) {
        
        Browser.mainLoop.running = true;
      }
      if (mode == 0 /*EM_TIMING_SETTIMEOUT*/) {
        Browser.mainLoop.scheduler = function Browser_mainLoop_scheduler_setTimeout() {
          var timeUntilNextTick = Math.max(0, Browser.mainLoop.tickStartTime + value - _emscripten_get_now())|0;
          setTimeout(Browser.mainLoop.runner, timeUntilNextTick); // doing this each time means that on exception, we stop
        };
        Browser.mainLoop.method = 'timeout';
      } else if (mode == 1 /*EM_TIMING_RAF*/) {
        Browser.mainLoop.scheduler = function Browser_mainLoop_scheduler_rAF() {
          Browser.requestAnimationFrame(Browser.mainLoop.runner);
        };
        Browser.mainLoop.method = 'rAF';
      } else if (mode == 2 /*EM_TIMING_SETIMMEDIATE*/) {
        if (typeof setImmediate == 'undefined') {
          // Emulate setImmediate. (note: not a complete polyfill, we don't emulate clearImmediate() to keep code size to minimum, since not needed)
          var setImmediates = [];
          var emscriptenMainLoopMessageId = 'setimmediate';
          var Browser_setImmediate_messageHandler = function(/** @type {Event} */ event) {
            // When called in current thread or Worker, the main loop ID is structured slightly different to accommodate for --proxy-to-worker runtime listening to Worker events,
            // so check for both cases.
            if (event.data === emscriptenMainLoopMessageId || event.data.target === emscriptenMainLoopMessageId) {
              event.stopPropagation();
              setImmediates.shift()();
            }
          }
          addEventListener("message", Browser_setImmediate_messageHandler, true);
          setImmediate = /** @type{function(function(): ?, ...?): number} */(function Browser_emulated_setImmediate(func) {
            setImmediates.push(func);
            if (ENVIRONMENT_IS_WORKER) {
              if (Module['setImmediates'] === undefined) Module['setImmediates'] = [];
              Module['setImmediates'].push(func);
              postMessage({target: emscriptenMainLoopMessageId}); // In --proxy-to-worker, route the message via proxyClient.js
            } else postMessage(emscriptenMainLoopMessageId, "*"); // On the main thread, can just send the message to itself.
          })
        }
        Browser.mainLoop.scheduler = function Browser_mainLoop_scheduler_setImmediate() {
          setImmediate(Browser.mainLoop.runner);
        };
        Browser.mainLoop.method = 'immediate';
      }
      return 0;
    }
  
  function maybeExit() {
    }
  
    /**
     * @param {number=} arg
     * @param {boolean=} noSetTiming
     */
  function setMainLoop(browserIterationFunc, fps, simulateInfiniteLoop, arg, noSetTiming) {
      assert(!Browser.mainLoop.func, 'emscripten_set_main_loop: there can only be one main loop function at once: call emscripten_cancel_main_loop to cancel the previous one before setting a new one with different parameters.');
  
      Browser.mainLoop.func = browserIterationFunc;
      Browser.mainLoop.arg = arg;
  
      var thisMainLoopId = Browser.mainLoop.currentlyRunningMainloop;
      function checkIsRunning() {
        if (thisMainLoopId < Browser.mainLoop.currentlyRunningMainloop) {
          
          maybeExit();
          return false;
        }
        return true;
      }
  
      // We create the loop runner here but it is not actually running until
      // _emscripten_set_main_loop_timing is called (which might happen a
      // later time).  This member signifies that the current runner has not
      // yet been started so that we can call runtimeKeepalivePush when it
      // gets it timing set for the first time.
      Browser.mainLoop.running = false;
      Browser.mainLoop.runner = function Browser_mainLoop_runner() {
        if (ABORT) return;
        if (Browser.mainLoop.queue.length > 0) {
          var start = Date.now();
          var blocker = Browser.mainLoop.queue.shift();
          blocker.func(blocker.arg);
          if (Browser.mainLoop.remainingBlockers) {
            var remaining = Browser.mainLoop.remainingBlockers;
            var next = remaining%1 == 0 ? remaining-1 : Math.floor(remaining);
            if (blocker.counted) {
              Browser.mainLoop.remainingBlockers = next;
            } else {
              // not counted, but move the progress along a tiny bit
              next = next + 0.5; // do not steal all the next one's progress
              Browser.mainLoop.remainingBlockers = (8*remaining + next)/9;
            }
          }
          out('main loop blocker "' + blocker.name + '" took ' + (Date.now() - start) + ' ms'); //, left: ' + Browser.mainLoop.remainingBlockers);
          Browser.mainLoop.updateStatus();
  
          // catches pause/resume main loop from blocker execution
          if (!checkIsRunning()) return;
  
          setTimeout(Browser.mainLoop.runner, 0);
          return;
        }
  
        // catch pauses from non-main loop sources
        if (!checkIsRunning()) return;
  
        // Implement very basic swap interval control
        Browser.mainLoop.currentFrameNumber = Browser.mainLoop.currentFrameNumber + 1 | 0;
        if (Browser.mainLoop.timingMode == 1/*EM_TIMING_RAF*/ && Browser.mainLoop.timingValue > 1 && Browser.mainLoop.currentFrameNumber % Browser.mainLoop.timingValue != 0) {
          // Not the scheduled time to render this frame - skip.
          Browser.mainLoop.scheduler();
          return;
        } else if (Browser.mainLoop.timingMode == 0/*EM_TIMING_SETTIMEOUT*/) {
          Browser.mainLoop.tickStartTime = _emscripten_get_now();
        }
  
        // Signal GL rendering layer that processing of a new frame is about to start. This helps it optimize
        // VBO double-buffering and reduce GPU stalls.
  
        Browser.mainLoop.runIter(browserIterationFunc);
  
        // catch pauses from the main loop itself
        if (!checkIsRunning()) return;
  
        // Queue new audio data. This is important to be right after the main loop invocation, so that we will immediately be able
        // to queue the newest produced audio samples.
        // TODO: Consider adding pre- and post- rAF callbacks so that GL.newRenderingFrameStarted() and SDL.audio.queueNewAudioData()
        //       do not need to be hardcoded into this function, but can be more generic.
        if (typeof SDL == 'object' && SDL.audio && SDL.audio.queueNewAudioData) SDL.audio.queueNewAudioData();
  
        Browser.mainLoop.scheduler();
      }
  
      if (!noSetTiming) {
        if (fps && fps > 0) _emscripten_set_main_loop_timing(0/*EM_TIMING_SETTIMEOUT*/, 1000.0 / fps);
        else _emscripten_set_main_loop_timing(1/*EM_TIMING_RAF*/, 1); // Do rAF by rendering each frame (no decimating)
  
        Browser.mainLoop.scheduler();
      }
  
      if (simulateInfiniteLoop) {
        throw 'unwind';
      }
    }
  var Browser = {mainLoop:{running:false,scheduler:null,method:"",currentlyRunningMainloop:0,func:null,arg:0,timingMode:0,timingValue:0,currentFrameNumber:0,queue:[],pause:function() {
          Browser.mainLoop.scheduler = null;
          // Incrementing this signals the previous main loop that it's now become old, and it must return.
          Browser.mainLoop.currentlyRunningMainloop++;
        },resume:function() {
          Browser.mainLoop.currentlyRunningMainloop++;
          var timingMode = Browser.mainLoop.timingMode;
          var timingValue = Browser.mainLoop.timingValue;
          var func = Browser.mainLoop.func;
          Browser.mainLoop.func = null;
          // do not set timing and call scheduler, we will do it on the next lines
          setMainLoop(func, 0, false, Browser.mainLoop.arg, true);
          _emscripten_set_main_loop_timing(timingMode, timingValue);
          Browser.mainLoop.scheduler();
        },updateStatus:function() {
          if (Module['setStatus']) {
            var message = Module['statusMessage'] || 'Please wait...';
            var remaining = Browser.mainLoop.remainingBlockers;
            var expected = Browser.mainLoop.expectedBlockers;
            if (remaining) {
              if (remaining < expected) {
                Module['setStatus'](message + ' (' + (expected - remaining) + '/' + expected + ')');
              } else {
                Module['setStatus'](message);
              }
            } else {
              Module['setStatus']('');
            }
          }
        },runIter:function(func) {
          if (ABORT) return;
          if (Module['preMainLoop']) {
            var preRet = Module['preMainLoop']();
            if (preRet === false) {
              return; // |return false| skips a frame
            }
          }
          callUserCallback(func);
          if (Module['postMainLoop']) Module['postMainLoop']();
        }},isFullscreen:false,pointerLock:false,moduleContextCreatedCallbacks:[],workers:[],init:function() {
        if (!Module["preloadPlugins"]) Module["preloadPlugins"] = []; // needs to exist even in workers
  
        if (Browser.initted) return;
        Browser.initted = true;
  
        try {
          new Blob();
          Browser.hasBlobConstructor = true;
        } catch(e) {
          Browser.hasBlobConstructor = false;
          out("warning: no blob constructor, cannot create blobs with mimetypes");
        }
        Browser.BlobBuilder = typeof MozBlobBuilder != "undefined" ? MozBlobBuilder : (typeof WebKitBlobBuilder != "undefined" ? WebKitBlobBuilder : (!Browser.hasBlobConstructor ? out("warning: no BlobBuilder") : null));
        Browser.URLObject = typeof window != "undefined" ? (window.URL ? window.URL : window.webkitURL) : undefined;
        if (!Module.noImageDecoding && typeof Browser.URLObject == 'undefined') {
          out("warning: Browser does not support creating object URLs. Built-in browser image decoding will not be available.");
          Module.noImageDecoding = true;
        }
  
        // Support for plugins that can process preloaded files. You can add more of these to
        // your app by creating and appending to Module.preloadPlugins.
        //
        // Each plugin is asked if it can handle a file based on the file's name. If it can,
        // it is given the file's raw data. When it is done, it calls a callback with the file's
        // (possibly modified) data. For example, a plugin might decompress a file, or it
        // might create some side data structure for use later (like an Image element, etc.).
  
        var imagePlugin = {};
        imagePlugin['canHandle'] = function imagePlugin_canHandle(name) {
          return !Module.noImageDecoding && /\.(jpg|jpeg|png|bmp)$/i.test(name);
        };
        imagePlugin['handle'] = function imagePlugin_handle(byteArray, name, onload, onerror) {
          var b = null;
          if (Browser.hasBlobConstructor) {
            try {
              b = new Blob([byteArray], { type: Browser.getMimetype(name) });
              if (b.size !== byteArray.length) { // Safari bug #118630
                // Safari's Blob can only take an ArrayBuffer
                b = new Blob([(new Uint8Array(byteArray)).buffer], { type: Browser.getMimetype(name) });
              }
            } catch(e) {
              warnOnce('Blob constructor present but fails: ' + e + '; falling back to blob builder');
            }
          }
          if (!b) {
            var bb = new Browser.BlobBuilder();
            bb.append((new Uint8Array(byteArray)).buffer); // we need to pass a buffer, and must copy the array to get the right data range
            b = bb.getBlob();
          }
          var url = Browser.URLObject.createObjectURL(b);
          var img = new Image();
          img.onload = () => {
            assert(img.complete, 'Image ' + name + ' could not be decoded');
            var canvas = /** @type {!HTMLCanvasElement} */ (document.createElement('canvas'));
            canvas.width = img.width;
            canvas.height = img.height;
            var ctx = canvas.getContext('2d');
            ctx.drawImage(img, 0, 0);
            preloadedImages[name] = canvas;
            Browser.URLObject.revokeObjectURL(url);
            if (onload) onload(byteArray);
          };
          img.onerror = (event) => {
            out('Image ' + url + ' could not be decoded');
            if (onerror) onerror();
          };
          img.src = url;
        };
        Module['preloadPlugins'].push(imagePlugin);
  
        var audioPlugin = {};
        audioPlugin['canHandle'] = function audioPlugin_canHandle(name) {
          return !Module.noAudioDecoding && name.substr(-4) in { '.ogg': 1, '.wav': 1, '.mp3': 1 };
        };
        audioPlugin['handle'] = function audioPlugin_handle(byteArray, name, onload, onerror) {
          var done = false;
          function finish(audio) {
            if (done) return;
            done = true;
            preloadedAudios[name] = audio;
            if (onload) onload(byteArray);
          }
          function fail() {
            if (done) return;
            done = true;
            preloadedAudios[name] = new Audio(); // empty shim
            if (onerror) onerror();
          }
          if (Browser.hasBlobConstructor) {
            try {
              var b = new Blob([byteArray], { type: Browser.getMimetype(name) });
            } catch(e) {
              return fail();
            }
            var url = Browser.URLObject.createObjectURL(b); // XXX we never revoke this!
            var audio = new Audio();
            audio.addEventListener('canplaythrough', function() { finish(audio) }, false); // use addEventListener due to chromium bug 124926
            audio.onerror = function audio_onerror(event) {
              if (done) return;
              out('warning: browser could not fully decode audio ' + name + ', trying slower base64 approach');
              function encode64(data) {
                var BASE = 'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/';
                var PAD = '=';
                var ret = '';
                var leftchar = 0;
                var leftbits = 0;
                for (var i = 0; i < data.length; i++) {
                  leftchar = (leftchar << 8) | data[i];
                  leftbits += 8;
                  while (leftbits >= 6) {
                    var curr = (leftchar >> (leftbits-6)) & 0x3f;
                    leftbits -= 6;
                    ret += BASE[curr];
                  }
                }
                if (leftbits == 2) {
                  ret += BASE[(leftchar&3) << 4];
                  ret += PAD + PAD;
                } else if (leftbits == 4) {
                  ret += BASE[(leftchar&0xf) << 2];
                  ret += PAD;
                }
                return ret;
              }
              audio.src = 'data:audio/x-' + name.substr(-3) + ';base64,' + encode64(byteArray);
              finish(audio); // we don't wait for confirmation this worked - but it's worth trying
            };
            audio.src = url;
            // workaround for chrome bug 124926 - we do not always get oncanplaythrough or onerror
            safeSetTimeout(function() {
              finish(audio); // try to use it even though it is not necessarily ready to play
            }, 10000);
          } else {
            return fail();
          }
        };
        Module['preloadPlugins'].push(audioPlugin);
  
        // Canvas event setup
  
        function pointerLockChange() {
          Browser.pointerLock = document['pointerLockElement'] === Module['canvas'] ||
                                document['mozPointerLockElement'] === Module['canvas'] ||
                                document['webkitPointerLockElement'] === Module['canvas'] ||
                                document['msPointerLockElement'] === Module['canvas'];
        }
        var canvas = Module['canvas'];
        if (canvas) {
          // forced aspect ratio can be enabled by defining 'forcedAspectRatio' on Module
          // Module['forcedAspectRatio'] = 4 / 3;
  
          canvas.requestPointerLock = canvas['requestPointerLock'] ||
                                      canvas['mozRequestPointerLock'] ||
                                      canvas['webkitRequestPointerLock'] ||
                                      canvas['msRequestPointerLock'] ||
                                      function(){};
          canvas.exitPointerLock = document['exitPointerLock'] ||
                                   document['mozExitPointerLock'] ||
                                   document['webkitExitPointerLock'] ||
                                   document['msExitPointerLock'] ||
                                   function(){}; // no-op if function does not exist
          canvas.exitPointerLock = canvas.exitPointerLock.bind(document);
  
          document.addEventListener('pointerlockchange', pointerLockChange, false);
          document.addEventListener('mozpointerlockchange', pointerLockChange, false);
          document.addEventListener('webkitpointerlockchange', pointerLockChange, false);
          document.addEventListener('mspointerlockchange', pointerLockChange, false);
  
          if (Module['elementPointerLock']) {
            canvas.addEventListener("click", function(ev) {
              if (!Browser.pointerLock && Module['canvas'].requestPointerLock) {
                Module['canvas'].requestPointerLock();
                ev.preventDefault();
              }
            }, false);
          }
        }
      },handledByPreloadPlugin:function(byteArray, fullname, finish, onerror) {
        // Ensure plugins are ready.
        Browser.init();
  
        var handled = false;
        Module['preloadPlugins'].forEach(function(plugin) {
          if (handled) return;
          if (plugin['canHandle'](fullname)) {
            plugin['handle'](byteArray, fullname, finish, onerror);
            handled = true;
          }
        });
        return handled;
      },createContext:function(/** @type {HTMLCanvasElement} */ canvas, useWebGL, setInModule, webGLContextAttributes) {
        if (useWebGL && Module.ctx && canvas == Module.canvas) return Module.ctx; // no need to recreate GL context if it's already been created for this canvas.
  
        var ctx;
        var contextHandle;
        if (useWebGL) {
          // For GLES2/desktop GL compatibility, adjust a few defaults to be different to WebGL defaults, so that they align better with the desktop defaults.
          var contextAttributes = {
            antialias: false,
            alpha: false,
            majorVersion: 1,
          };
  
          if (webGLContextAttributes) {
            for (var attribute in webGLContextAttributes) {
              contextAttributes[attribute] = webGLContextAttributes[attribute];
            }
          }
  
          // This check of existence of GL is here to satisfy Closure compiler, which yells if variable GL is referenced below but GL object is not
          // actually compiled in because application is not doing any GL operations. TODO: Ideally if GL is not being used, this function
          // Browser.createContext() should not even be emitted.
          if (typeof GL != 'undefined') {
            contextHandle = GL.createContext(canvas, contextAttributes);
            if (contextHandle) {
              ctx = GL.getContext(contextHandle).GLctx;
            }
          }
        } else {
          ctx = canvas.getContext('2d');
        }
  
        if (!ctx) return null;
  
        if (setInModule) {
          if (!useWebGL) assert(typeof GLctx == 'undefined', 'cannot set in module if GLctx is used, but we are a non-GL context that would replace it');
  
          Module.ctx = ctx;
          if (useWebGL) GL.makeContextCurrent(contextHandle);
          Module.useWebGL = useWebGL;
          Browser.moduleContextCreatedCallbacks.forEach(function(callback) { callback() });
          Browser.init();
        }
        return ctx;
      },destroyContext:function(canvas, useWebGL, setInModule) {},fullscreenHandlersInstalled:false,lockPointer:undefined,resizeCanvas:undefined,requestFullscreen:function(lockPointer, resizeCanvas) {
        Browser.lockPointer = lockPointer;
        Browser.resizeCanvas = resizeCanvas;
        if (typeof Browser.lockPointer == 'undefined') Browser.lockPointer = true;
        if (typeof Browser.resizeCanvas == 'undefined') Browser.resizeCanvas = false;
  
        var canvas = Module['canvas'];
        function fullscreenChange() {
          Browser.isFullscreen = false;
          var canvasContainer = canvas.parentNode;
          if ((document['fullscreenElement'] || document['mozFullScreenElement'] ||
               document['msFullscreenElement'] || document['webkitFullscreenElement'] ||
               document['webkitCurrentFullScreenElement']) === canvasContainer) {
            canvas.exitFullscreen = Browser.exitFullscreen;
            if (Browser.lockPointer) canvas.requestPointerLock();
            Browser.isFullscreen = true;
            if (Browser.resizeCanvas) {
              Browser.setFullscreenCanvasSize();
            } else {
              Browser.updateCanvasDimensions(canvas);
            }
          } else {
            // remove the full screen specific parent of the canvas again to restore the HTML structure from before going full screen
            canvasContainer.parentNode.insertBefore(canvas, canvasContainer);
            canvasContainer.parentNode.removeChild(canvasContainer);
  
            if (Browser.resizeCanvas) {
              Browser.setWindowedCanvasSize();
            } else {
              Browser.updateCanvasDimensions(canvas);
            }
          }
          if (Module['onFullScreen']) Module['onFullScreen'](Browser.isFullscreen);
          if (Module['onFullscreen']) Module['onFullscreen'](Browser.isFullscreen);
        }
  
        if (!Browser.fullscreenHandlersInstalled) {
          Browser.fullscreenHandlersInstalled = true;
          document.addEventListener('fullscreenchange', fullscreenChange, false);
          document.addEventListener('mozfullscreenchange', fullscreenChange, false);
          document.addEventListener('webkitfullscreenchange', fullscreenChange, false);
          document.addEventListener('MSFullscreenChange', fullscreenChange, false);
        }
  
        // create a new parent to ensure the canvas has no siblings. this allows browsers to optimize full screen performance when its parent is the full screen root
        var canvasContainer = document.createElement("div");
        canvas.parentNode.insertBefore(canvasContainer, canvas);
        canvasContainer.appendChild(canvas);
  
        // use parent of canvas as full screen root to allow aspect ratio correction (Firefox stretches the root to screen size)
        canvasContainer.requestFullscreen = canvasContainer['requestFullscreen'] ||
                                            canvasContainer['mozRequestFullScreen'] ||
                                            canvasContainer['msRequestFullscreen'] ||
                                           (canvasContainer['webkitRequestFullscreen'] ? function() { canvasContainer['webkitRequestFullscreen'](Element['ALLOW_KEYBOARD_INPUT']) } : null) ||
                                           (canvasContainer['webkitRequestFullScreen'] ? function() { canvasContainer['webkitRequestFullScreen'](Element['ALLOW_KEYBOARD_INPUT']) } : null);
  
        canvasContainer.requestFullscreen();
      },exitFullscreen:function() {
        // This is workaround for chrome. Trying to exit from fullscreen
        // not in fullscreen state will cause "TypeError: Document not active"
        // in chrome. See https://github.com/emscripten-core/emscripten/pull/8236
        if (!Browser.isFullscreen) {
          return false;
        }
  
        var CFS = document['exitFullscreen'] ||
                  document['cancelFullScreen'] ||
                  document['mozCancelFullScreen'] ||
                  document['msExitFullscreen'] ||
                  document['webkitCancelFullScreen'] ||
            (function() {});
        CFS.apply(document, []);
        return true;
      },nextRAF:0,fakeRequestAnimationFrame:function(func) {
        // try to keep 60fps between calls to here
        var now = Date.now();
        if (Browser.nextRAF === 0) {
          Browser.nextRAF = now + 1000/60;
        } else {
          while (now + 2 >= Browser.nextRAF) { // fudge a little, to avoid timer jitter causing us to do lots of delay:0
            Browser.nextRAF += 1000/60;
          }
        }
        var delay = Math.max(Browser.nextRAF - now, 0);
        setTimeout(func, delay);
      },requestAnimationFrame:function(func) {
        if (typeof requestAnimationFrame == 'function') {
          requestAnimationFrame(func);
          return;
        }
        var RAF = Browser.fakeRequestAnimationFrame;
        RAF(func);
      },safeSetTimeout:function(func) {
        // Legacy function, this is used by the SDL2 port so we need to keep it
        // around at least until that is updated.
        return safeSetTimeout(func);
      },safeRequestAnimationFrame:function(func) {
        
        return Browser.requestAnimationFrame(function() {
          
          callUserCallback(func);
        });
      },getMimetype:function(name) {
        return {
          'jpg': 'image/jpeg',
          'jpeg': 'image/jpeg',
          'png': 'image/png',
          'bmp': 'image/bmp',
          'ogg': 'audio/ogg',
          'wav': 'audio/wav',
          'mp3': 'audio/mpeg'
        }[name.substr(name.lastIndexOf('.')+1)];
      },getUserMedia:function(func) {
        if (!window.getUserMedia) {
          window.getUserMedia = navigator['getUserMedia'] ||
                                navigator['mozGetUserMedia'];
        }
        window.getUserMedia(func);
      },getMovementX:function(event) {
        return event['movementX'] ||
               event['mozMovementX'] ||
               event['webkitMovementX'] ||
               0;
      },getMovementY:function(event) {
        return event['movementY'] ||
               event['mozMovementY'] ||
               event['webkitMovementY'] ||
               0;
      },getMouseWheelDelta:function(event) {
        var delta = 0;
        switch (event.type) {
          case 'DOMMouseScroll':
            // 3 lines make up a step
            delta = event.detail / 3;
            break;
          case 'mousewheel':
            // 120 units make up a step
            delta = event.wheelDelta / 120;
            break;
          case 'wheel':
            delta = event.deltaY
            switch (event.deltaMode) {
              case 0:
                // DOM_DELTA_PIXEL: 100 pixels make up a step
                delta /= 100;
                break;
              case 1:
                // DOM_DELTA_LINE: 3 lines make up a step
                delta /= 3;
                break;
              case 2:
                // DOM_DELTA_PAGE: A page makes up 80 steps
                delta *= 80;
                break;
              default:
                throw 'unrecognized mouse wheel delta mode: ' + event.deltaMode;
            }
            break;
          default:
            throw 'unrecognized mouse wheel event: ' + event.type;
        }
        return delta;
      },mouseX:0,mouseY:0,mouseMovementX:0,mouseMovementY:0,touches:{},lastTouches:{},calculateMouseEvent:function(event) { // event should be mousemove, mousedown or mouseup
        if (Browser.pointerLock) {
          // When the pointer is locked, calculate the coordinates
          // based on the movement of the mouse.
          // Workaround for Firefox bug 764498
          if (event.type != 'mousemove' &&
              ('mozMovementX' in event)) {
            Browser.mouseMovementX = Browser.mouseMovementY = 0;
          } else {
            Browser.mouseMovementX = Browser.getMovementX(event);
            Browser.mouseMovementY = Browser.getMovementY(event);
          }
  
          // check if SDL is available
          if (typeof SDL != "undefined") {
            Browser.mouseX = SDL.mouseX + Browser.mouseMovementX;
            Browser.mouseY = SDL.mouseY + Browser.mouseMovementY;
          } else {
            // just add the mouse delta to the current absolut mouse position
            // FIXME: ideally this should be clamped against the canvas size and zero
            Browser.mouseX += Browser.mouseMovementX;
            Browser.mouseY += Browser.mouseMovementY;
          }
        } else {
          // Otherwise, calculate the movement based on the changes
          // in the coordinates.
          var rect = Module["canvas"].getBoundingClientRect();
          var cw = Module["canvas"].width;
          var ch = Module["canvas"].height;
  
          // Neither .scrollX or .pageXOffset are defined in a spec, but
          // we prefer .scrollX because it is currently in a spec draft.
          // (see: http://www.w3.org/TR/2013/WD-cssom-view-20131217/)
          var scrollX = ((typeof window.scrollX != 'undefined') ? window.scrollX : window.pageXOffset);
          var scrollY = ((typeof window.scrollY != 'undefined') ? window.scrollY : window.pageYOffset);
  
          if (event.type === 'touchstart' || event.type === 'touchend' || event.type === 'touchmove') {
            var touch = event.touch;
            if (touch === undefined) {
              return; // the "touch" property is only defined in SDL
  
            }
            var adjustedX = touch.pageX - (scrollX + rect.left);
            var adjustedY = touch.pageY - (scrollY + rect.top);
  
            adjustedX = adjustedX * (cw / rect.width);
            adjustedY = adjustedY * (ch / rect.height);
  
            var coords = { x: adjustedX, y: adjustedY };
  
            if (event.type === 'touchstart') {
              Browser.lastTouches[touch.identifier] = coords;
              Browser.touches[touch.identifier] = coords;
            } else if (event.type === 'touchend' || event.type === 'touchmove') {
              var last = Browser.touches[touch.identifier];
              if (!last) last = coords;
              Browser.lastTouches[touch.identifier] = last;
              Browser.touches[touch.identifier] = coords;
            }
            return;
          }
  
          var x = event.pageX - (scrollX + rect.left);
          var y = event.pageY - (scrollY + rect.top);
  
          // the canvas might be CSS-scaled compared to its backbuffer;
          // SDL-using content will want mouse coordinates in terms
          // of backbuffer units.
          x = x * (cw / rect.width);
          y = y * (ch / rect.height);
  
          Browser.mouseMovementX = x - Browser.mouseX;
          Browser.mouseMovementY = y - Browser.mouseY;
          Browser.mouseX = x;
          Browser.mouseY = y;
        }
      },resizeListeners:[],updateResizeListeners:function() {
        var canvas = Module['canvas'];
        Browser.resizeListeners.forEach(function(listener) {
          listener(canvas.width, canvas.height);
        });
      },setCanvasSize:function(width, height, noUpdates) {
        var canvas = Module['canvas'];
        Browser.updateCanvasDimensions(canvas, width, height);
        if (!noUpdates) Browser.updateResizeListeners();
      },windowedWidth:0,windowedHeight:0,setFullscreenCanvasSize:function() {
        // check if SDL is available
        if (typeof SDL != "undefined") {
          var flags = HEAPU32[((SDL.screen)>>2)];
          flags = flags | 0x00800000; // set SDL_FULLSCREEN flag
          HEAP32[((SDL.screen)>>2)] = flags;
        }
        Browser.updateCanvasDimensions(Module['canvas']);
        Browser.updateResizeListeners();
      },setWindowedCanvasSize:function() {
        // check if SDL is available
        if (typeof SDL != "undefined") {
          var flags = HEAPU32[((SDL.screen)>>2)];
          flags = flags & ~0x00800000; // clear SDL_FULLSCREEN flag
          HEAP32[((SDL.screen)>>2)] = flags;
        }
        Browser.updateCanvasDimensions(Module['canvas']);
        Browser.updateResizeListeners();
      },updateCanvasDimensions:function(canvas, wNative, hNative) {
        if (wNative && hNative) {
          canvas.widthNative = wNative;
          canvas.heightNative = hNative;
        } else {
          wNative = canvas.widthNative;
          hNative = canvas.heightNative;
        }
        var w = wNative;
        var h = hNative;
        if (Module['forcedAspectRatio'] && Module['forcedAspectRatio'] > 0) {
          if (w/h < Module['forcedAspectRatio']) {
            w = Math.round(h * Module['forcedAspectRatio']);
          } else {
            h = Math.round(w / Module['forcedAspectRatio']);
          }
        }
        if (((document['fullscreenElement'] || document['mozFullScreenElement'] ||
             document['msFullscreenElement'] || document['webkitFullscreenElement'] ||
             document['webkitCurrentFullScreenElement']) === canvas.parentNode) && (typeof screen != 'undefined')) {
           var factor = Math.min(screen.width / w, screen.height / h);
           w = Math.round(w * factor);
           h = Math.round(h * factor);
        }
        if (Browser.resizeCanvas) {
          if (canvas.width  != w) canvas.width  = w;
          if (canvas.height != h) canvas.height = h;
          if (typeof canvas.style != 'undefined') {
            canvas.style.removeProperty( "width");
            canvas.style.removeProperty("height");
          }
        } else {
          if (canvas.width  != wNative) canvas.width  = wNative;
          if (canvas.height != hNative) canvas.height = hNative;
          if (typeof canvas.style != 'undefined') {
            if (w != wNative || h != hNative) {
              canvas.style.setProperty( "width", w + "px", "important");
              canvas.style.setProperty("height", h + "px", "important");
            } else {
              canvas.style.removeProperty( "width");
              canvas.style.removeProperty("height");
            }
          }
        }
      }};
  
  /** @constructor */
  function GLFW_Window(id, width, height, title, monitor, share) {
        this.id = id;
        this.x = 0;
        this.y = 0;
        this.fullscreen = false; // Used to determine if app in fullscreen mode
        this.storedX = 0; // Used to store X before fullscreen
        this.storedY = 0; // Used to store Y before fullscreen
        this.width = width;
        this.height = height;
        this.storedWidth = width; // Used to store width before fullscreen
        this.storedHeight = height; // Used to store height before fullscreen
        this.title = title;
        this.monitor = monitor;
        this.share = share;
        this.attributes = GLFW.hints;
        this.inputModes = {
          0x00033001:0x00034001, // GLFW_CURSOR (GLFW_CURSOR_NORMAL)
          0x00033002:0, // GLFW_STICKY_KEYS
          0x00033003:0, // GLFW_STICKY_MOUSE_BUTTONS
        };
        this.buttons = 0;
        this.keys = new Array();
        this.domKeys = new Array();
        this.shouldClose = 0;
        this.title = null;
        this.windowPosFunc = null; // GLFWwindowposfun
        this.windowSizeFunc = null; // GLFWwindowsizefun
        this.windowCloseFunc = null; // GLFWwindowclosefun
        this.windowRefreshFunc = null; // GLFWwindowrefreshfun
        this.windowFocusFunc = null; // GLFWwindowfocusfun
        this.windowIconifyFunc = null; // GLFWwindowiconifyfun
        this.framebufferSizeFunc = null; // GLFWframebuffersizefun
        this.mouseButtonFunc = null; // GLFWmousebuttonfun
        this.cursorPosFunc = null; // GLFWcursorposfun
        this.cursorEnterFunc = null; // GLFWcursorenterfun
        this.scrollFunc = null; // GLFWscrollfun
        this.dropFunc = null; // GLFWdropfun
        this.keyFunc = null; // GLFWkeyfun
        this.charFunc = null; // GLFWcharfun
        this.userptr = null;
      }
  var GLFW = {WindowFromId:function(id) {
        if (id <= 0 || !GLFW.windows) return null;
        return GLFW.windows[id - 1];
      },joystickFunc:null,errorFunc:null,monitorFunc:null,active:null,windows:null,monitors:null,monitorString:null,versionString:null,initialTime:null,extensions:null,hints:null,defaultHints:{131073:0,131074:0,131075:1,131076:1,131077:1,135169:8,135170:8,135171:8,135172:8,135173:24,135174:8,135175:0,135176:0,135177:0,135178:0,135179:0,135180:0,135181:0,135182:0,135183:0,139265:196609,139266:1,139267:0,139268:0,139269:0,139270:0,139271:0,139272:0},DOMToGLFWKeyCode:function(keycode) {
        switch (keycode) {
          // these keycodes are only defined for GLFW3, assume they are the same for GLFW2
          case 0x20:return 32; // DOM_VK_SPACE -> GLFW_KEY_SPACE
          case 0xDE:return 39; // DOM_VK_QUOTE -> GLFW_KEY_APOSTROPHE
          case 0xBC:return 44; // DOM_VK_COMMA -> GLFW_KEY_COMMA
          case 0xAD:return 45; // DOM_VK_HYPHEN_MINUS -> GLFW_KEY_MINUS
          case 0xBD:return 45; // DOM_VK_MINUS -> GLFW_KEY_MINUS
          case 0xBE:return 46; // DOM_VK_PERIOD -> GLFW_KEY_PERIOD
          case 0xBF:return 47; // DOM_VK_SLASH -> GLFW_KEY_SLASH
          case 0x30:return 48; // DOM_VK_0 -> GLFW_KEY_0
          case 0x31:return 49; // DOM_VK_1 -> GLFW_KEY_1
          case 0x32:return 50; // DOM_VK_2 -> GLFW_KEY_2
          case 0x33:return 51; // DOM_VK_3 -> GLFW_KEY_3
          case 0x34:return 52; // DOM_VK_4 -> GLFW_KEY_4
          case 0x35:return 53; // DOM_VK_5 -> GLFW_KEY_5
          case 0x36:return 54; // DOM_VK_6 -> GLFW_KEY_6
          case 0x37:return 55; // DOM_VK_7 -> GLFW_KEY_7
          case 0x38:return 56; // DOM_VK_8 -> GLFW_KEY_8
          case 0x39:return 57; // DOM_VK_9 -> GLFW_KEY_9
          case 0x3B:return 59; // DOM_VK_SEMICOLON -> GLFW_KEY_SEMICOLON
          case 0x3D:return 61; // DOM_VK_EQUALS -> GLFW_KEY_EQUAL
          case 0xBB:return 61; // DOM_VK_EQUALS -> GLFW_KEY_EQUAL
          case 0x41:return 65; // DOM_VK_A -> GLFW_KEY_A
          case 0x42:return 66; // DOM_VK_B -> GLFW_KEY_B
          case 0x43:return 67; // DOM_VK_C -> GLFW_KEY_C
          case 0x44:return 68; // DOM_VK_D -> GLFW_KEY_D
          case 0x45:return 69; // DOM_VK_E -> GLFW_KEY_E
          case 0x46:return 70; // DOM_VK_F -> GLFW_KEY_F
          case 0x47:return 71; // DOM_VK_G -> GLFW_KEY_G
          case 0x48:return 72; // DOM_VK_H -> GLFW_KEY_H
          case 0x49:return 73; // DOM_VK_I -> GLFW_KEY_I
          case 0x4A:return 74; // DOM_VK_J -> GLFW_KEY_J
          case 0x4B:return 75; // DOM_VK_K -> GLFW_KEY_K
          case 0x4C:return 76; // DOM_VK_L -> GLFW_KEY_L
          case 0x4D:return 77; // DOM_VK_M -> GLFW_KEY_M
          case 0x4E:return 78; // DOM_VK_N -> GLFW_KEY_N
          case 0x4F:return 79; // DOM_VK_O -> GLFW_KEY_O
          case 0x50:return 80; // DOM_VK_P -> GLFW_KEY_P
          case 0x51:return 81; // DOM_VK_Q -> GLFW_KEY_Q
          case 0x52:return 82; // DOM_VK_R -> GLFW_KEY_R
          case 0x53:return 83; // DOM_VK_S -> GLFW_KEY_S
          case 0x54:return 84; // DOM_VK_T -> GLFW_KEY_T
          case 0x55:return 85; // DOM_VK_U -> GLFW_KEY_U
          case 0x56:return 86; // DOM_VK_V -> GLFW_KEY_V
          case 0x57:return 87; // DOM_VK_W -> GLFW_KEY_W
          case 0x58:return 88; // DOM_VK_X -> GLFW_KEY_X
          case 0x59:return 89; // DOM_VK_Y -> GLFW_KEY_Y
          case 0x5a:return 90; // DOM_VK_Z -> GLFW_KEY_Z
          case 0xDB:return 91; // DOM_VK_OPEN_BRACKET -> GLFW_KEY_LEFT_BRACKET
          case 0xDC:return 92; // DOM_VK_BACKSLASH -> GLFW_KEY_BACKSLASH
          case 0xDD:return 93; // DOM_VK_CLOSE_BRACKET -> GLFW_KEY_RIGHT_BRACKET
          case 0xC0:return 96; // DOM_VK_BACK_QUOTE -> GLFW_KEY_GRAVE_ACCENT
  
          case 0x1B:return 256; // DOM_VK_ESCAPE -> GLFW_KEY_ESCAPE
          case 0x0D:return 257; // DOM_VK_RETURN -> GLFW_KEY_ENTER
          case 0x09:return 258; // DOM_VK_TAB -> GLFW_KEY_TAB
          case 0x08:return 259; // DOM_VK_BACK -> GLFW_KEY_BACKSPACE
          case 0x2D:return 260; // DOM_VK_INSERT -> GLFW_KEY_INSERT
          case 0x2E:return 261; // DOM_VK_DELETE -> GLFW_KEY_DELETE
          case 0x27:return 262; // DOM_VK_RIGHT -> GLFW_KEY_RIGHT
          case 0x25:return 263; // DOM_VK_LEFT -> GLFW_KEY_LEFT
          case 0x28:return 264; // DOM_VK_DOWN -> GLFW_KEY_DOWN
          case 0x26:return 265; // DOM_VK_UP -> GLFW_KEY_UP
          case 0x21:return 266; // DOM_VK_PAGE_UP -> GLFW_KEY_PAGE_UP
          case 0x22:return 267; // DOM_VK_PAGE_DOWN -> GLFW_KEY_PAGE_DOWN
          case 0x24:return 268; // DOM_VK_HOME -> GLFW_KEY_HOME
          case 0x23:return 269; // DOM_VK_END -> GLFW_KEY_END
          case 0x14:return 280; // DOM_VK_CAPS_LOCK -> GLFW_KEY_CAPS_LOCK
          case 0x91:return 281; // DOM_VK_SCROLL_LOCK -> GLFW_KEY_SCROLL_LOCK
          case 0x90:return 282; // DOM_VK_NUM_LOCK -> GLFW_KEY_NUM_LOCK
          case 0x2C:return 283; // DOM_VK_SNAPSHOT -> GLFW_KEY_PRINT_SCREEN
          case 0x13:return 284; // DOM_VK_PAUSE -> GLFW_KEY_PAUSE
          case 0x70:return 290; // DOM_VK_F1 -> GLFW_KEY_F1
          case 0x71:return 291; // DOM_VK_F2 -> GLFW_KEY_F2
          case 0x72:return 292; // DOM_VK_F3 -> GLFW_KEY_F3
          case 0x73:return 293; // DOM_VK_F4 -> GLFW_KEY_F4
          case 0x74:return 294; // DOM_VK_F5 -> GLFW_KEY_F5
          case 0x75:return 295; // DOM_VK_F6 -> GLFW_KEY_F6
          case 0x76:return 296; // DOM_VK_F7 -> GLFW_KEY_F7
          case 0x77:return 297; // DOM_VK_F8 -> GLFW_KEY_F8
          case 0x78:return 298; // DOM_VK_F9 -> GLFW_KEY_F9
          case 0x79:return 299; // DOM_VK_F10 -> GLFW_KEY_F10
          case 0x7A:return 300; // DOM_VK_F11 -> GLFW_KEY_F11
          case 0x7B:return 301; // DOM_VK_F12 -> GLFW_KEY_F12
          case 0x7C:return 302; // DOM_VK_F13 -> GLFW_KEY_F13
          case 0x7D:return 303; // DOM_VK_F14 -> GLFW_KEY_F14
          case 0x7E:return 304; // DOM_VK_F15 -> GLFW_KEY_F15
          case 0x7F:return 305; // DOM_VK_F16 -> GLFW_KEY_F16
          case 0x80:return 306; // DOM_VK_F17 -> GLFW_KEY_F17
          case 0x81:return 307; // DOM_VK_F18 -> GLFW_KEY_F18
          case 0x82:return 308; // DOM_VK_F19 -> GLFW_KEY_F19
          case 0x83:return 309; // DOM_VK_F20 -> GLFW_KEY_F20
          case 0x84:return 310; // DOM_VK_F21 -> GLFW_KEY_F21
          case 0x85:return 311; // DOM_VK_F22 -> GLFW_KEY_F22
          case 0x86:return 312; // DOM_VK_F23 -> GLFW_KEY_F23
          case 0x87:return 313; // DOM_VK_F24 -> GLFW_KEY_F24
          case 0x88:return 314; // 0x88 (not used?) -> GLFW_KEY_F25
          case 0x60:return 320; // DOM_VK_NUMPAD0 -> GLFW_KEY_KP_0
          case 0x61:return 321; // DOM_VK_NUMPAD1 -> GLFW_KEY_KP_1
          case 0x62:return 322; // DOM_VK_NUMPAD2 -> GLFW_KEY_KP_2
          case 0x63:return 323; // DOM_VK_NUMPAD3 -> GLFW_KEY_KP_3
          case 0x64:return 324; // DOM_VK_NUMPAD4 -> GLFW_KEY_KP_4
          case 0x65:return 325; // DOM_VK_NUMPAD5 -> GLFW_KEY_KP_5
          case 0x66:return 326; // DOM_VK_NUMPAD6 -> GLFW_KEY_KP_6
          case 0x67:return 327; // DOM_VK_NUMPAD7 -> GLFW_KEY_KP_7
          case 0x68:return 328; // DOM_VK_NUMPAD8 -> GLFW_KEY_KP_8
          case 0x69:return 329; // DOM_VK_NUMPAD9 -> GLFW_KEY_KP_9
          case 0x6E:return 330; // DOM_VK_DECIMAL -> GLFW_KEY_KP_DECIMAL
          case 0x6F:return 331; // DOM_VK_DIVIDE -> GLFW_KEY_KP_DIVIDE
          case 0x6A:return 332; // DOM_VK_MULTIPLY -> GLFW_KEY_KP_MULTIPLY
          case 0x6D:return 333; // DOM_VK_SUBTRACT -> GLFW_KEY_KP_SUBTRACT
          case 0x6B:return 334; // DOM_VK_ADD -> GLFW_KEY_KP_ADD
          // case 0x0D:return 335; // DOM_VK_RETURN -> GLFW_KEY_KP_ENTER (DOM_KEY_LOCATION_RIGHT)
          // case 0x61:return 336; // DOM_VK_EQUALS -> GLFW_KEY_KP_EQUAL (DOM_KEY_LOCATION_RIGHT)
          case 0x10:return 340; // DOM_VK_SHIFT -> GLFW_KEY_LEFT_SHIFT
          case 0x11:return 341; // DOM_VK_CONTROL -> GLFW_KEY_LEFT_CONTROL
          case 0x12:return 342; // DOM_VK_ALT -> GLFW_KEY_LEFT_ALT
          case 0x5B:return 343; // DOM_VK_WIN -> GLFW_KEY_LEFT_SUPER
          // case 0x10:return 344; // DOM_VK_SHIFT -> GLFW_KEY_RIGHT_SHIFT (DOM_KEY_LOCATION_RIGHT)
          // case 0x11:return 345; // DOM_VK_CONTROL -> GLFW_KEY_RIGHT_CONTROL (DOM_KEY_LOCATION_RIGHT)
          // case 0x12:return 346; // DOM_VK_ALT -> GLFW_KEY_RIGHT_ALT (DOM_KEY_LOCATION_RIGHT)
          // case 0x5B:return 347; // DOM_VK_WIN -> GLFW_KEY_RIGHT_SUPER (DOM_KEY_LOCATION_RIGHT)
          case 0x5D:return 348; // DOM_VK_CONTEXT_MENU -> GLFW_KEY_MENU
          // XXX: GLFW_KEY_WORLD_1, GLFW_KEY_WORLD_2 what are these?
          default:return -1; // GLFW_KEY_UNKNOWN
        };
      },getModBits:function(win) {
        var mod = 0;
        if (win.keys[340]) mod |= 0x0001; // GLFW_MOD_SHIFT
        if (win.keys[341]) mod |= 0x0002; // GLFW_MOD_CONTROL
        if (win.keys[342]) mod |= 0x0004; // GLFW_MOD_ALT
        if (win.keys[343]) mod |= 0x0008; // GLFW_MOD_SUPER
        return mod;
      },onKeyPress:function(event) {
        if (!GLFW.active || !GLFW.active.charFunc) return;
        if (event.ctrlKey || event.metaKey) return;
  
        // correct unicode charCode is only available with onKeyPress event
        var charCode = event.charCode;
        if (charCode == 0 || (charCode >= 0x00 && charCode <= 0x1F)) return;
  
        (function(a1, a2) {  dynCall_vii.apply(null, [GLFW.active.charFunc, a1, a2]); })(GLFW.active.id, charCode);
      },onKeyChanged:function(keyCode, status) {
        if (!GLFW.active) return;
  
        var key = GLFW.DOMToGLFWKeyCode(keyCode);
        if (key == -1) return;
  
        var repeat = status && GLFW.active.keys[key];
        GLFW.active.keys[key] = status;
        GLFW.active.domKeys[keyCode] = status;
        if (!GLFW.active.keyFunc) return;
  
        if (repeat) status = 2; // GLFW_REPEAT
        (function(a1, a2, a3, a4, a5) {  dynCall_viiiii.apply(null, [GLFW.active.keyFunc, a1, a2, a3, a4, a5]); })(GLFW.active.id, key, keyCode, status, GLFW.getModBits(GLFW.active));
      },onGamepadConnected:function(event) {
        GLFW.refreshJoysticks();
      },onGamepadDisconnected:function(event) {
        GLFW.refreshJoysticks();
      },onKeydown:function(event) {
        GLFW.onKeyChanged(event.keyCode, 1); // GLFW_PRESS or GLFW_REPEAT
  
        // This logic comes directly from the sdl implementation. We cannot
        // call preventDefault on all keydown events otherwise onKeyPress will
        // not get called
        if (event.keyCode === 8 /* backspace */ || event.keyCode === 9 /* tab */) {
          event.preventDefault();
        }
      },onKeyup:function(event) {
        GLFW.onKeyChanged(event.keyCode, 0); // GLFW_RELEASE
      },onBlur:function(event) {
        if (!GLFW.active) return;
  
        for (var i = 0; i < GLFW.active.domKeys.length; ++i) {
          if (GLFW.active.domKeys[i]) {
            GLFW.onKeyChanged(i, 0); // GLFW_RELEASE
          }
        }
      },onMousemove:function(event) {
        if (!GLFW.active) return;
  
        Browser.calculateMouseEvent(event);
  
        if (event.target != Module["canvas"] || !GLFW.active.cursorPosFunc) return;
  
        (function(a1, a2, a3) {  dynCall_vidd.apply(null, [GLFW.active.cursorPosFunc, a1, a2, a3]); })(GLFW.active.id, Browser.mouseX, Browser.mouseY);
      },DOMToGLFWMouseButton:function(event) {
        // DOM and glfw have different button codes.
        // See http://www.w3schools.com/jsref/event_button.asp.
        var eventButton = event['button'];
        if (eventButton > 0) {
          if (eventButton == 1) {
            eventButton = 2;
          } else {
            eventButton = 1;
          }
        }
        return eventButton;
      },onMouseenter:function(event) {
        if (!GLFW.active) return;
  
        if (event.target != Module["canvas"] || !GLFW.active.cursorEnterFunc) return;
  
        (function(a1, a2) {  dynCall_vii.apply(null, [GLFW.active.cursorEnterFunc, a1, a2]); })(GLFW.active.id, 1);
      },onMouseleave:function(event) {
        if (!GLFW.active) return;
  
        if (event.target != Module["canvas"] || !GLFW.active.cursorEnterFunc) return;
  
        (function(a1, a2) {  dynCall_vii.apply(null, [GLFW.active.cursorEnterFunc, a1, a2]); })(GLFW.active.id, 0);
      },onMouseButtonChanged:function(event, status) {
        if (!GLFW.active) return;
  
        Browser.calculateMouseEvent(event);
  
        if (event.target != Module["canvas"]) return;
  
        var eventButton = GLFW.DOMToGLFWMouseButton(event);
  
        if (status == 1) { // GLFW_PRESS
          GLFW.active.buttons |= (1 << eventButton);
          try {
            event.target.setCapture();
          } catch (e) {}
        } else {  // GLFW_RELEASE
          GLFW.active.buttons &= ~(1 << eventButton);
        }
  
        if (!GLFW.active.mouseButtonFunc) return;
  
        (function(a1, a2, a3, a4) {  dynCall_viiii.apply(null, [GLFW.active.mouseButtonFunc, a1, a2, a3, a4]); })(GLFW.active.id, eventButton, status, GLFW.getModBits(GLFW.active));
      },onMouseButtonDown:function(event) {
        if (!GLFW.active) return;
        GLFW.onMouseButtonChanged(event, 1); // GLFW_PRESS
      },onMouseButtonUp:function(event) {
        if (!GLFW.active) return;
        GLFW.onMouseButtonChanged(event, 0); // GLFW_RELEASE
      },onMouseWheel:function(event) {
        // Note the minus sign that flips browser wheel direction (positive direction scrolls page down) to native wheel direction (positive direction is mouse wheel up)
        var delta = -Browser.getMouseWheelDelta(event);
        delta = (delta == 0) ? 0 : (delta > 0 ? Math.max(delta, 1) : Math.min(delta, -1)); // Quantize to integer so that minimum scroll is at least +/- 1.
        GLFW.wheelPos += delta;
  
        if (!GLFW.active || !GLFW.active.scrollFunc || event.target != Module['canvas']) return;
  
        var sx = 0;
        var sy = delta;
        if (event.type == 'mousewheel') {
          sx = event.wheelDeltaX;
        } else {
          sx = event.deltaX;
        }
  
        (function(a1, a2, a3) {  dynCall_vidd.apply(null, [GLFW.active.scrollFunc, a1, a2, a3]); })(GLFW.active.id, sx, sy);
  
        event.preventDefault();
      },onCanvasResize:function(width, height) {
        if (!GLFW.active) return;
  
        var resizeNeeded = true;
  
        // If the client is requesting fullscreen mode
        if (document["fullscreen"] || document["fullScreen"] || document["mozFullScreen"] || document["webkitIsFullScreen"]) {
          GLFW.active.storedX = GLFW.active.x;
          GLFW.active.storedY = GLFW.active.y;
          GLFW.active.storedWidth = GLFW.active.width;
          GLFW.active.storedHeight = GLFW.active.height;
          GLFW.active.x = GLFW.active.y = 0;
          GLFW.active.width = screen.width;
          GLFW.active.height = screen.height;
          GLFW.active.fullscreen = true;
  
        // If the client is reverting from fullscreen mode
        } else if (GLFW.active.fullscreen == true) {
          GLFW.active.x = GLFW.active.storedX;
          GLFW.active.y = GLFW.active.storedY;
          GLFW.active.width = GLFW.active.storedWidth;
          GLFW.active.height = GLFW.active.storedHeight;
          GLFW.active.fullscreen = false;
  
        // If the width/height values do not match current active window sizes
        } else if (GLFW.active.width != width || GLFW.active.height != height) {
            GLFW.active.width = width;
            GLFW.active.height = height;
        } else {
          resizeNeeded = false;
        }
  
        // If any of the above conditions were true, we need to resize the canvas
        if (resizeNeeded) {
          // resets the canvas size to counter the aspect preservation of Browser.updateCanvasDimensions
          Browser.setCanvasSize(GLFW.active.width, GLFW.active.height, true);
          // TODO: Client dimensions (clientWidth/clientHeight) vs pixel dimensions (width/height) of
          // the canvas should drive window and framebuffer size respectfully.
          GLFW.onWindowSizeChanged();
          GLFW.onFramebufferSizeChanged();
        }
      },onWindowSizeChanged:function() {
        if (!GLFW.active) return;
  
        if (!GLFW.active.windowSizeFunc) return;
  
        callUserCallback(function() {
  
          (function(a1, a2, a3) {  dynCall_viii.apply(null, [GLFW.active.windowSizeFunc, a1, a2, a3]); })(GLFW.active.id, GLFW.active.width, GLFW.active.height);
        });
      },onFramebufferSizeChanged:function() {
        if (!GLFW.active) return;
  
        if (!GLFW.active.framebufferSizeFunc) return;
  
        callUserCallback(function() {
          (function(a1, a2, a3) {  dynCall_viii.apply(null, [GLFW.active.framebufferSizeFunc, a1, a2, a3]); })(GLFW.active.id, GLFW.active.width, GLFW.active.height);
        });
      },getTime:function() {
        return _emscripten_get_now() / 1000;
      },setWindowTitle:function(winid, title) {
        var win = GLFW.WindowFromId(winid);
        if (!win) return;
  
        win.title = UTF8ToString(title);
        if (GLFW.active.id == win.id) {
          document.title = win.title;
        }
      },setJoystickCallback:function(cbfun) {
        GLFW.joystickFunc = cbfun;
        GLFW.refreshJoysticks();
      },joys:{},lastGamepadState:[],lastGamepadStateFrame:null,refreshJoysticks:function() {
        // Produce a new Gamepad API sample if we are ticking a new game frame, or if not using emscripten_set_main_loop() at all to drive animation.
        if (Browser.mainLoop.currentFrameNumber !== GLFW.lastGamepadStateFrame || !Browser.mainLoop.currentFrameNumber) {
          GLFW.lastGamepadState = navigator.getGamepads ? navigator.getGamepads() : (navigator.webkitGetGamepads ? navigator.webkitGetGamepads : []);
          GLFW.lastGamepadStateFrame = Browser.mainLoop.currentFrameNumber;
  
          for (var joy = 0; joy < GLFW.lastGamepadState.length; ++joy) {
            var gamepad = GLFW.lastGamepadState[joy];
  
            if (gamepad) {
              if (!GLFW.joys[joy]) {
                out('glfw joystick connected:',joy);
                GLFW.joys[joy] = {
                  id: allocateUTF8(gamepad.id),
                  buttonsCount: gamepad.buttons.length,
                  axesCount: gamepad.axes.length,
                  buttons: _malloc(gamepad.buttons.length),
                  axes: _malloc(gamepad.axes.length*4),
                };
  
                if (GLFW.joystickFunc) {
                  (function(a1, a2) {  dynCall_vii.apply(null, [GLFW.joystickFunc, a1, a2]); })(joy, 0x00040001); // GLFW_CONNECTED
                }
              }
  
              var data = GLFW.joys[joy];
  
              for (var i = 0; i < gamepad.buttons.length;  ++i) {
                HEAP8[((data.buttons + i)>>0)] = gamepad.buttons[i].pressed;
              }
  
              for (var i = 0; i < gamepad.axes.length; ++i) {
                HEAPF32[((data.axes + i*4)>>2)] = gamepad.axes[i];
              }
            } else {
              if (GLFW.joys[joy]) {
                out('glfw joystick disconnected',joy);
  
                if (GLFW.joystickFunc) {
                  (function(a1, a2) {  dynCall_vii.apply(null, [GLFW.joystickFunc, a1, a2]); })(joy, 0x00040002); // GLFW_DISCONNECTED
                }
  
                _free(GLFW.joys[joy].id);
                _free(GLFW.joys[joy].buttons);
                _free(GLFW.joys[joy].axes);
  
                delete GLFW.joys[joy];
              }
            }
          }
        }
      },setKeyCallback:function(winid, cbfun) {
        var win = GLFW.WindowFromId(winid);
        if (!win) return null;
        var prevcbfun = win.keyFunc;
        win.keyFunc = cbfun;
        return prevcbfun;
      },setCharCallback:function(winid, cbfun) {
        var win = GLFW.WindowFromId(winid);
        if (!win) return null;
        var prevcbfun = win.charFunc;
        win.charFunc = cbfun;
        return prevcbfun;
      },setMouseButtonCallback:function(winid, cbfun) {
        var win = GLFW.WindowFromId(winid);
        if (!win) return null;
        var prevcbfun = win.mouseButtonFunc;
        win.mouseButtonFunc = cbfun;
        return prevcbfun;
      },setCursorPosCallback:function(winid, cbfun) {
        var win = GLFW.WindowFromId(winid);
        if (!win) return null;
        var prevcbfun = win.cursorPosFunc;
        win.cursorPosFunc = cbfun;
        return prevcbfun;
      },setScrollCallback:function(winid, cbfun) {
        var win = GLFW.WindowFromId(winid);
        if (!win) return null;
        var prevcbfun = win.scrollFunc;
        win.scrollFunc = cbfun;
        return prevcbfun;
      },setDropCallback:function(winid, cbfun) {
        var win = GLFW.WindowFromId(winid);
        if (!win) return null;
        var prevcbfun = win.dropFunc;
        win.dropFunc = cbfun;
        return prevcbfun;
      },onDrop:function(event) {
        if (!GLFW.active || !GLFW.active.dropFunc) return;
        if (!event.dataTransfer || !event.dataTransfer.files || event.dataTransfer.files.length == 0) return;
  
        event.preventDefault();
  
        var filenames = _malloc(event.dataTransfer.files.length*4);
        var filenamesArray = [];
        var count = event.dataTransfer.files.length;
  
        // Read and save the files to emscripten's FS
        var written = 0;
        var drop_dir = '.glfw_dropped_files';
        FS.createPath('/', drop_dir);
  
        function save(file) {
          var path = '/' + drop_dir + '/' + file.name.replace(/\//g, '_');
          var reader = new FileReader();
          reader.onloadend = (e) => {
            if (reader.readyState != 2) { // not DONE
              ++written;
              out('failed to read dropped file: '+file.name+': '+reader.error);
              return;
            }
  
            var data = e.target.result;
            FS.writeFile(path, new Uint8Array(data));
            if (++written === count) {
              (function(a1, a2, a3) {  dynCall_viii.apply(null, [GLFW.active.dropFunc, a1, a2, a3]); })(GLFW.active.id, count, filenames);
  
              for (var i = 0; i < filenamesArray.length; ++i) {
                _free(filenamesArray[i]);
              }
              _free(filenames);
            }
          };
          reader.readAsArrayBuffer(file);
  
          var filename = allocateUTF8(path);
          filenamesArray.push(filename);
          HEAPU32[((filenames + i*4)>>2)] = filename;
        }
  
        for (var i = 0; i < count; ++i) {
          save(event.dataTransfer.files[i]);
        }
  
        return false;
      },onDragover:function(event) {
        if (!GLFW.active || !GLFW.active.dropFunc) return;
  
        event.preventDefault();
        return false;
      },setWindowSizeCallback:function(winid, cbfun) {
        var win = GLFW.WindowFromId(winid);
        if (!win) return null;
        var prevcbfun = win.windowSizeFunc;
        win.windowSizeFunc = cbfun;
  
        return prevcbfun;
      },setWindowCloseCallback:function(winid, cbfun) {
        var win = GLFW.WindowFromId(winid);
        if (!win) return null;
        var prevcbfun = win.windowCloseFunc;
        win.windowCloseFunc = cbfun;
        return prevcbfun;
      },setWindowRefreshCallback:function(winid, cbfun) {
        var win = GLFW.WindowFromId(winid);
        if (!win) return null;
        var prevcbfun = win.windowRefreshFunc;
        win.windowRefreshFunc = cbfun;
        return prevcbfun;
      },onClickRequestPointerLock:function(e) {
        if (!Browser.pointerLock && Module['canvas'].requestPointerLock) {
          Module['canvas'].requestPointerLock();
          e.preventDefault();
        }
      },setInputMode:function(winid, mode, value) {
        var win = GLFW.WindowFromId(winid);
        if (!win) return;
  
        switch (mode) {
          case 0x00033001: { // GLFW_CURSOR
            switch (value) {
              case 0x00034001: { // GLFW_CURSOR_NORMAL
                win.inputModes[mode] = value;
                Module['canvas'].removeEventListener('click', GLFW.onClickRequestPointerLock, true);
                Module['canvas'].exitPointerLock();
                break;
              }
              case 0x00034002: { // GLFW_CURSOR_HIDDEN
                out("glfwSetInputMode called with GLFW_CURSOR_HIDDEN value not implemented.");
                break;
              }
              case 0x00034003: { // GLFW_CURSOR_DISABLED
                win.inputModes[mode] = value;
                Module['canvas'].addEventListener('click', GLFW.onClickRequestPointerLock, true);
                Module['canvas'].requestPointerLock();
                break;
              }
              default: {
                out("glfwSetInputMode called with unknown value parameter value: " + value + ".");
                break;
              }
            }
            break;
          }
          case 0x00033002: { // GLFW_STICKY_KEYS
            out("glfwSetInputMode called with GLFW_STICKY_KEYS mode not implemented.");
            break;
          }
          case 0x00033003: { // GLFW_STICKY_MOUSE_BUTTONS
            out("glfwSetInputMode called with GLFW_STICKY_MOUSE_BUTTONS mode not implemented.");
            break;
          }
          default: {
            out("glfwSetInputMode called with unknown mode parameter value: " + mode + ".");
            break;
          }
        }
      },getKey:function(winid, key) {
        var win = GLFW.WindowFromId(winid);
        if (!win) return 0;
        return win.keys[key];
      },getMouseButton:function(winid, button) {
        var win = GLFW.WindowFromId(winid);
        if (!win) return 0;
        return (win.buttons & (1 << button)) > 0;
      },getCursorPos:function(winid, x, y) {
        HEAPF64[((x)>>3)] = Browser.mouseX;
        HEAPF64[((y)>>3)] = Browser.mouseY;
      },getMousePos:function(winid, x, y) {
        HEAP32[((x)>>2)] = Browser.mouseX;
        HEAP32[((y)>>2)] = Browser.mouseY;
      },setCursorPos:function(winid, x, y) {
      },getWindowPos:function(winid, x, y) {
        var wx = 0;
        var wy = 0;
  
        var win = GLFW.WindowFromId(winid);
        if (win) {
          wx = win.x;
          wy = win.y;
        }
  
        if (x) {
          HEAP32[((x)>>2)] = wx;
        }
  
        if (y) {
          HEAP32[((y)>>2)] = wy;
        }
      },setWindowPos:function(winid, x, y) {
        var win = GLFW.WindowFromId(winid);
        if (!win) return;
        win.x = x;
        win.y = y;
      },getWindowSize:function(winid, width, height) {
        var ww = 0;
        var wh = 0;
  
        var win = GLFW.WindowFromId(winid);
        if (win) {
          ww = win.width;
          wh = win.height;
        }
  
        if (width) {
          HEAP32[((width)>>2)] = ww;
        }
  
        if (height) {
          HEAP32[((height)>>2)] = wh;
        }
      },setWindowSize:function(winid, width, height) {
        var win = GLFW.WindowFromId(winid);
        if (!win) return;
  
        if (GLFW.active.id == win.id) {
          if (width == screen.width && height == screen.height) {
            Browser.requestFullscreen();
          } else {
            Browser.exitFullscreen();
            Browser.setCanvasSize(width, height);
            win.width = width;
            win.height = height;
          }
        }
  
        if (!win.windowSizeFunc) return;
  
        (function(a1, a2, a3) {  dynCall_viii.apply(null, [win.windowSizeFunc, a1, a2, a3]); })(win.id, width, height);
      },createWindow:function(width, height, title, monitor, share) {
        var i, id;
        for (i = 0; i < GLFW.windows.length && GLFW.windows[i] !== null; i++) {
          // no-op
        }
        if (i > 0) throw "glfwCreateWindow only supports one window at time currently";
  
        // id for window
        id = i + 1;
  
        // not valid
        if (width <= 0 || height <= 0) return 0;
  
        if (monitor) {
          Browser.requestFullscreen();
        } else {
          Browser.setCanvasSize(width, height);
        }
  
        // Create context when there are no existing alive windows
        for (i = 0; i < GLFW.windows.length && GLFW.windows[i] == null; i++) {
          // no-op
        }
        var useWebGL = GLFW.hints[0x00022001] > 0; // Use WebGL when we are told to based on GLFW_CLIENT_API
        if (i == GLFW.windows.length) {
          if (useWebGL) {
            var contextAttributes = {
              antialias: (GLFW.hints[0x0002100D] > 1), // GLFW_SAMPLES
              depth: (GLFW.hints[0x00021005] > 0),     // GLFW_DEPTH_BITS
              stencil: (GLFW.hints[0x00021006] > 0),   // GLFW_STENCIL_BITS
              alpha: (GLFW.hints[0x00021004] > 0)      // GLFW_ALPHA_BITS
            }
            Module.ctx = Browser.createContext(Module['canvas'], true, true, contextAttributes);
          } else {
            Browser.init();
          }
        }
  
        // If context creation failed, do not return a valid window
        if (!Module.ctx && useWebGL) return 0;
  
        // Get non alive id
        var win = new GLFW_Window(id, width, height, title, monitor, share);
  
        // Set window to array
        if (id - 1 == GLFW.windows.length) {
          GLFW.windows.push(win);
        } else {
          GLFW.windows[id - 1] = win;
        }
  
        GLFW.active = win;
        return win.id;
      },destroyWindow:function(winid) {
        var win = GLFW.WindowFromId(winid);
        if (!win) return;
  
        if (win.windowCloseFunc)
          (function(a1) {  dynCall_vi.apply(null, [win.windowCloseFunc, a1]); })(win.id);
  
        GLFW.windows[win.id - 1] = null;
        if (GLFW.active.id == win.id)
          GLFW.active = null;
  
        // Destroy context when no alive windows
        for (var i = 0; i < GLFW.windows.length; i++)
          if (GLFW.windows[i] !== null) return;
  
        Module.ctx = Browser.destroyContext(Module['canvas'], true, true);
      },swapBuffers:function(winid) {
      },GLFW2ParamToGLFW3Param:function(param) {
        var table = {
          0x00030001:0, // GLFW_MOUSE_CURSOR
          0x00030002:0, // GLFW_STICKY_KEYS
          0x00030003:0, // GLFW_STICKY_MOUSE_BUTTONS
          0x00030004:0, // GLFW_SYSTEM_KEYS
          0x00030005:0, // GLFW_KEY_REPEAT
          0x00030006:0, // GLFW_AUTO_POLL_EVENTS
          0x00020001:0, // GLFW_OPENED
          0x00020002:0, // GLFW_ACTIVE
          0x00020003:0, // GLFW_ICONIFIED
          0x00020004:0, // GLFW_ACCELERATED
          0x00020005:0x00021001, // GLFW_RED_BITS
          0x00020006:0x00021002, // GLFW_GREEN_BITS
          0x00020007:0x00021003, // GLFW_BLUE_BITS
          0x00020008:0x00021004, // GLFW_ALPHA_BITS
          0x00020009:0x00021005, // GLFW_DEPTH_BITS
          0x0002000A:0x00021006, // GLFW_STENCIL_BITS
          0x0002000B:0x0002100F, // GLFW_REFRESH_RATE
          0x0002000C:0x00021007, // GLFW_ACCUM_RED_BITS
          0x0002000D:0x00021008, // GLFW_ACCUM_GREEN_BITS
          0x0002000E:0x00021009, // GLFW_ACCUM_BLUE_BITS
          0x0002000F:0x0002100A, // GLFW_ACCUM_ALPHA_BITS
          0x00020010:0x0002100B, // GLFW_AUX_BUFFERS
          0x00020011:0x0002100C, // GLFW_STEREO
          0x00020012:0, // GLFW_WINDOW_NO_RESIZE
          0x00020013:0x0002100D, // GLFW_FSAA_SAMPLES
          0x00020014:0x00022002, // GLFW_OPENGL_VERSION_MAJOR
          0x00020015:0x00022003, // GLFW_OPENGL_VERSION_MINOR
          0x00020016:0x00022006, // GLFW_OPENGL_FORWARD_COMPAT
          0x00020017:0x00022007, // GLFW_OPENGL_DEBUG_CONTEXT
          0x00020018:0x00022008, // GLFW_OPENGL_PROFILE
        };
        return table[param];
      }};
  function _glfwCreateWindow(width, height, title, monitor, share) {
      return GLFW.createWindow(width, height, title, monitor, share);
    }

  function _glfwDefaultWindowHints() {
      GLFW.hints = GLFW.defaultHints;
    }

  function _glfwDestroyWindow(winid) {
      return GLFW.destroyWindow(winid);
    }

  function _glfwGetPrimaryMonitor() {
      return 1;
    }

  function _glfwGetTime() {
      return GLFW.getTime() - GLFW.initialTime;
    }

  function _glfwGetVideoModes(monitor, count) {
      HEAP32[((count)>>2)] = 0;
      return 0;
    }

  function _glfwInit() {
      if (GLFW.windows) return 1; // GL_TRUE
  
      GLFW.initialTime = GLFW.getTime();
      GLFW.hints = GLFW.defaultHints;
      GLFW.windows = new Array()
      GLFW.active = null;
  
      window.addEventListener("gamepadconnected", GLFW.onGamepadConnected, true);
      window.addEventListener("gamepaddisconnected", GLFW.onGamepadDisconnected, true);
      window.addEventListener("keydown", GLFW.onKeydown, true);
      window.addEventListener("keypress", GLFW.onKeyPress, true);
      window.addEventListener("keyup", GLFW.onKeyup, true);
      window.addEventListener("blur", GLFW.onBlur, true);
      Module["canvas"].addEventListener("touchmove", GLFW.onMousemove, true);
      Module["canvas"].addEventListener("touchstart", GLFW.onMouseButtonDown, true);
      Module["canvas"].addEventListener("touchcancel", GLFW.onMouseButtonUp, true);
      Module["canvas"].addEventListener("touchend", GLFW.onMouseButtonUp, true);
      Module["canvas"].addEventListener("mousemove", GLFW.onMousemove, true);
      Module["canvas"].addEventListener("mousedown", GLFW.onMouseButtonDown, true);
      Module["canvas"].addEventListener("mouseup", GLFW.onMouseButtonUp, true);
      Module["canvas"].addEventListener('wheel', GLFW.onMouseWheel, true);
      Module["canvas"].addEventListener('mousewheel', GLFW.onMouseWheel, true);
      Module["canvas"].addEventListener('mouseenter', GLFW.onMouseenter, true);
      Module["canvas"].addEventListener('mouseleave', GLFW.onMouseleave, true);
      Module["canvas"].addEventListener('drop', GLFW.onDrop, true);
      Module["canvas"].addEventListener('dragover', GLFW.onDragover, true);
  
      Browser.resizeListeners.push(function(width, height) {
         GLFW.onCanvasResize(width, height);
      });
      return 1; // GL_TRUE
    }

  function _glfwMakeContextCurrent(winid) {}

  function _glfwSetCharCallback(winid, cbfun) {
      return GLFW.setCharCallback(winid, cbfun);
    }

  function _glfwSetCursorEnterCallback(winid, cbfun) {
      var win = GLFW.WindowFromId(winid);
      if (!win) return null;
      var prevcbfun = win.cursorEnterFunc;
      win.cursorEnterFunc = cbfun;
      return prevcbfun;
    }

  function _glfwSetCursorPosCallback(winid, cbfun) {
      return GLFW.setCursorPosCallback(winid, cbfun);
    }

  function _glfwSetDropCallback(winid, cbfun) {
      return GLFW.setDropCallback(winid, cbfun);
    }

  function _glfwSetErrorCallback(cbfun) {
      var prevcbfun = GLFW.errorFunc;
      GLFW.errorFunc = cbfun;
      return prevcbfun;
    }

  function _glfwSetKeyCallback(winid, cbfun) {
      return GLFW.setKeyCallback(winid, cbfun);
    }

  function _glfwSetMouseButtonCallback(winid, cbfun) {
      return GLFW.setMouseButtonCallback(winid, cbfun);
    }

  function _glfwSetScrollCallback(winid, cbfun) {
      return GLFW.setScrollCallback(winid, cbfun);
    }

  function _glfwSetWindowFocusCallback(winid, cbfun) {
      var win = GLFW.WindowFromId(winid);
      if (!win) return null;
      var prevcbfun = win.windowFocusFunc;
      win.windowFocusFunc = cbfun;
      return prevcbfun;
    }

  function _glfwSetWindowIconifyCallback(winid, cbfun) {
      var win = GLFW.WindowFromId(winid);
      if (!win) return null;
      var prevcbfun = win.windowIconifyFunc;
      win.windowIconifyFunc = cbfun;
      return prevcbfun;
    }

  function _glfwSetWindowShouldClose(winid, value) {
      var win = GLFW.WindowFromId(winid);
      if (!win) return;
      win.shouldClose = value;
    }

  function _glfwSetWindowSizeCallback(winid, cbfun) {
      return GLFW.setWindowSizeCallback(winid, cbfun);
    }

  function _glfwSwapBuffers(winid) {
      GLFW.swapBuffers(winid);
    }

  function _glfwSwapInterval(interval) {
      interval = Math.abs(interval); // GLFW uses negative values to enable GLX_EXT_swap_control_tear, which we don't have, so just treat negative and positive the same.
      if (interval == 0) _emscripten_set_main_loop_timing(0/*EM_TIMING_SETTIMEOUT*/, 0);
      else _emscripten_set_main_loop_timing(1/*EM_TIMING_RAF*/, interval);
    }

  function _glfwTerminate() {
      window.removeEventListener("gamepadconnected", GLFW.onGamepadConnected, true);
      window.removeEventListener("gamepaddisconnected", GLFW.onGamepadDisconnected, true);
      window.removeEventListener("keydown", GLFW.onKeydown, true);
      window.removeEventListener("keypress", GLFW.onKeyPress, true);
      window.removeEventListener("keyup", GLFW.onKeyup, true);
      window.removeEventListener("blur", GLFW.onBlur, true);
      Module["canvas"].removeEventListener("touchmove", GLFW.onMousemove, true);
      Module["canvas"].removeEventListener("touchstart", GLFW.onMouseButtonDown, true);
      Module["canvas"].removeEventListener("touchcancel", GLFW.onMouseButtonUp, true);
      Module["canvas"].removeEventListener("touchend", GLFW.onMouseButtonUp, true);
      Module["canvas"].removeEventListener("mousemove", GLFW.onMousemove, true);
      Module["canvas"].removeEventListener("mousedown", GLFW.onMouseButtonDown, true);
      Module["canvas"].removeEventListener("mouseup", GLFW.onMouseButtonUp, true);
      Module["canvas"].removeEventListener('wheel', GLFW.onMouseWheel, true);
      Module["canvas"].removeEventListener('mousewheel', GLFW.onMouseWheel, true);
      Module["canvas"].removeEventListener('mouseenter', GLFW.onMouseenter, true);
      Module["canvas"].removeEventListener('mouseleave', GLFW.onMouseleave, true);
      Module["canvas"].removeEventListener('drop', GLFW.onDrop, true);
      Module["canvas"].removeEventListener('dragover', GLFW.onDragover, true);
  
      Module["canvas"].width = Module["canvas"].height = 1;
      GLFW.windows = null;
      GLFW.active = null;
    }

  function _glfwWindowHint(target, hint) {
      GLFW.hints[target] = hint;
    }

  function _llvm_eh_typeid_for(type) {
      return type;
    }

  var DOTNET = {};
  function _mono_set_timeout(
  ) {
  return __dotnet_runtime.__linker_exports.mono_set_timeout.apply(__dotnet_runtime, arguments)
  }

  function _mono_wasm_add_dbg_command_received(
  ) {
  return __dotnet_runtime.__linker_exports.mono_wasm_add_dbg_command_received.apply(__dotnet_runtime, arguments)
  }

  function _mono_wasm_asm_loaded(
  ) {
  return __dotnet_runtime.__linker_exports.mono_wasm_asm_loaded.apply(__dotnet_runtime, arguments)
  }

  function _mono_wasm_bind_cs_function(
  ) {
  return __dotnet_runtime.__linker_exports.mono_wasm_bind_cs_function.apply(__dotnet_runtime, arguments)
  }

  function _mono_wasm_bind_js_function(
  ) {
  return __dotnet_runtime.__linker_exports.mono_wasm_bind_js_function.apply(__dotnet_runtime, arguments)
  }

  function _mono_wasm_create_cs_owned_object_ref(
  ) {
  return __dotnet_runtime.__linker_exports.mono_wasm_create_cs_owned_object_ref.apply(__dotnet_runtime, arguments)
  }

  function _mono_wasm_debugger_log(
  ) {
  return __dotnet_runtime.__linker_exports.mono_wasm_debugger_log.apply(__dotnet_runtime, arguments)
  }

  function _mono_wasm_fire_debugger_agent_message(
  ) {
  return __dotnet_runtime.__linker_exports.mono_wasm_fire_debugger_agent_message.apply(__dotnet_runtime, arguments)
  }

  function _mono_wasm_get_by_index_ref(
  ) {
  return __dotnet_runtime.__linker_exports.mono_wasm_get_by_index_ref.apply(__dotnet_runtime, arguments)
  }

  function _mono_wasm_get_global_object_ref(
  ) {
  return __dotnet_runtime.__linker_exports.mono_wasm_get_global_object_ref.apply(__dotnet_runtime, arguments)
  }

  function _mono_wasm_get_object_property_ref(
  ) {
  return __dotnet_runtime.__linker_exports.mono_wasm_get_object_property_ref.apply(__dotnet_runtime, arguments)
  }

  function _mono_wasm_invoke_bound_function(
  ) {
  return __dotnet_runtime.__linker_exports.mono_wasm_invoke_bound_function.apply(__dotnet_runtime, arguments)
  }

  function _mono_wasm_invoke_js_blazor(
  ) {
  return __dotnet_runtime.__linker_exports.mono_wasm_invoke_js_blazor.apply(__dotnet_runtime, arguments)
  }

  function _mono_wasm_invoke_js_with_args_ref(
  ) {
  return __dotnet_runtime.__linker_exports.mono_wasm_invoke_js_with_args_ref.apply(__dotnet_runtime, arguments)
  }

  function _mono_wasm_marshal_promise(
  ) {
  return __dotnet_runtime.__linker_exports.mono_wasm_marshal_promise.apply(__dotnet_runtime, arguments)
  }

  function _mono_wasm_release_cs_owned_object(
  ) {
  return __dotnet_runtime.__linker_exports.mono_wasm_release_cs_owned_object.apply(__dotnet_runtime, arguments)
  }

  function _mono_wasm_set_by_index_ref(
  ) {
  return __dotnet_runtime.__linker_exports.mono_wasm_set_by_index_ref.apply(__dotnet_runtime, arguments)
  }

  function _mono_wasm_set_entrypoint_breakpoint(
  ) {
  return __dotnet_runtime.__linker_exports.mono_wasm_set_entrypoint_breakpoint.apply(__dotnet_runtime, arguments)
  }

  function _mono_wasm_set_object_property_ref(
  ) {
  return __dotnet_runtime.__linker_exports.mono_wasm_set_object_property_ref.apply(__dotnet_runtime, arguments)
  }

  function _mono_wasm_trace_logger(
  ) {
  return __dotnet_runtime.__linker_exports.mono_wasm_trace_logger.apply(__dotnet_runtime, arguments)
  }

  function _mono_wasm_typed_array_from_ref(
  ) {
  return __dotnet_runtime.__linker_exports.mono_wasm_typed_array_from_ref.apply(__dotnet_runtime, arguments)
  }

  function _mono_wasm_typed_array_to_array_ref(
  ) {
  return __dotnet_runtime.__linker_exports.mono_wasm_typed_array_to_array_ref.apply(__dotnet_runtime, arguments)
  }

  function _schedule_background_exec(
  ) {
  return __dotnet_runtime.__linker_exports.schedule_background_exec.apply(__dotnet_runtime, arguments)
  }

  function _setTempRet0(val) {
      setTempRet0(val);
    }

  function __isLeapYear(year) {
        return year%4 === 0 && (year%100 !== 0 || year%400 === 0);
    }
  
  function __arraySum(array, index) {
      var sum = 0;
      for (var i = 0; i <= index; sum += array[i++]) {
        // no-op
      }
      return sum;
    }
  
  var __MONTH_DAYS_LEAP = [31,29,31,30,31,30,31,31,30,31,30,31];
  
  var __MONTH_DAYS_REGULAR = [31,28,31,30,31,30,31,31,30,31,30,31];
  function __addDays(date, days) {
      var newDate = new Date(date.getTime());
      while (days > 0) {
        var leap = __isLeapYear(newDate.getFullYear());
        var currentMonth = newDate.getMonth();
        var daysInCurrentMonth = (leap ? __MONTH_DAYS_LEAP : __MONTH_DAYS_REGULAR)[currentMonth];
  
        if (days > daysInCurrentMonth-newDate.getDate()) {
          // we spill over to next month
          days -= (daysInCurrentMonth-newDate.getDate()+1);
          newDate.setDate(1);
          if (currentMonth < 11) {
            newDate.setMonth(currentMonth+1)
          } else {
            newDate.setMonth(0);
            newDate.setFullYear(newDate.getFullYear()+1);
          }
        } else {
          // we stay in current month
          newDate.setDate(newDate.getDate()+days);
          return newDate;
        }
      }
  
      return newDate;
    }
  function _strftime(s, maxsize, format, tm) {
      // size_t strftime(char *restrict s, size_t maxsize, const char *restrict format, const struct tm *restrict timeptr);
      // http://pubs.opengroup.org/onlinepubs/009695399/functions/strftime.html
  
      var tm_zone = HEAP32[(((tm)+(40))>>2)];
  
      var date = {
        tm_sec: HEAP32[((tm)>>2)],
        tm_min: HEAP32[(((tm)+(4))>>2)],
        tm_hour: HEAP32[(((tm)+(8))>>2)],
        tm_mday: HEAP32[(((tm)+(12))>>2)],
        tm_mon: HEAP32[(((tm)+(16))>>2)],
        tm_year: HEAP32[(((tm)+(20))>>2)],
        tm_wday: HEAP32[(((tm)+(24))>>2)],
        tm_yday: HEAP32[(((tm)+(28))>>2)],
        tm_isdst: HEAP32[(((tm)+(32))>>2)],
        tm_gmtoff: HEAP32[(((tm)+(36))>>2)],
        tm_zone: tm_zone ? UTF8ToString(tm_zone) : ''
      };
  
      var pattern = UTF8ToString(format);
  
      // expand format
      var EXPANSION_RULES_1 = {
        '%c': '%a %b %d %H:%M:%S %Y',     // Replaced by the locale's appropriate date and time representation - e.g., Mon Aug  3 14:02:01 2013
        '%D': '%m/%d/%y',                 // Equivalent to %m / %d / %y
        '%F': '%Y-%m-%d',                 // Equivalent to %Y - %m - %d
        '%h': '%b',                       // Equivalent to %b
        '%r': '%I:%M:%S %p',              // Replaced by the time in a.m. and p.m. notation
        '%R': '%H:%M',                    // Replaced by the time in 24-hour notation
        '%T': '%H:%M:%S',                 // Replaced by the time
        '%x': '%m/%d/%y',                 // Replaced by the locale's appropriate date representation
        '%X': '%H:%M:%S',                 // Replaced by the locale's appropriate time representation
        // Modified Conversion Specifiers
        '%Ec': '%c',                      // Replaced by the locale's alternative appropriate date and time representation.
        '%EC': '%C',                      // Replaced by the name of the base year (period) in the locale's alternative representation.
        '%Ex': '%m/%d/%y',                // Replaced by the locale's alternative date representation.
        '%EX': '%H:%M:%S',                // Replaced by the locale's alternative time representation.
        '%Ey': '%y',                      // Replaced by the offset from %EC (year only) in the locale's alternative representation.
        '%EY': '%Y',                      // Replaced by the full alternative year representation.
        '%Od': '%d',                      // Replaced by the day of the month, using the locale's alternative numeric symbols, filled as needed with leading zeros if there is any alternative symbol for zero; otherwise, with leading <space> characters.
        '%Oe': '%e',                      // Replaced by the day of the month, using the locale's alternative numeric symbols, filled as needed with leading <space> characters.
        '%OH': '%H',                      // Replaced by the hour (24-hour clock) using the locale's alternative numeric symbols.
        '%OI': '%I',                      // Replaced by the hour (12-hour clock) using the locale's alternative numeric symbols.
        '%Om': '%m',                      // Replaced by the month using the locale's alternative numeric symbols.
        '%OM': '%M',                      // Replaced by the minutes using the locale's alternative numeric symbols.
        '%OS': '%S',                      // Replaced by the seconds using the locale's alternative numeric symbols.
        '%Ou': '%u',                      // Replaced by the weekday as a number in the locale's alternative representation (Monday=1).
        '%OU': '%U',                      // Replaced by the week number of the year (Sunday as the first day of the week, rules corresponding to %U ) using the locale's alternative numeric symbols.
        '%OV': '%V',                      // Replaced by the week number of the year (Monday as the first day of the week, rules corresponding to %V ) using the locale's alternative numeric symbols.
        '%Ow': '%w',                      // Replaced by the number of the weekday (Sunday=0) using the locale's alternative numeric symbols.
        '%OW': '%W',                      // Replaced by the week number of the year (Monday as the first day of the week) using the locale's alternative numeric symbols.
        '%Oy': '%y',                      // Replaced by the year (offset from %C ) using the locale's alternative numeric symbols.
      };
      for (var rule in EXPANSION_RULES_1) {
        pattern = pattern.replace(new RegExp(rule, 'g'), EXPANSION_RULES_1[rule]);
      }
  
      var WEEKDAYS = ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday'];
      var MONTHS = ['January', 'February', 'March', 'April', 'May', 'June', 'July', 'August', 'September', 'October', 'November', 'December'];
  
      function leadingSomething(value, digits, character) {
        var str = typeof value == 'number' ? value.toString() : (value || '');
        while (str.length < digits) {
          str = character[0]+str;
        }
        return str;
      }
  
      function leadingNulls(value, digits) {
        return leadingSomething(value, digits, '0');
      }
  
      function compareByDay(date1, date2) {
        function sgn(value) {
          return value < 0 ? -1 : (value > 0 ? 1 : 0);
        }
  
        var compare;
        if ((compare = sgn(date1.getFullYear()-date2.getFullYear())) === 0) {
          if ((compare = sgn(date1.getMonth()-date2.getMonth())) === 0) {
            compare = sgn(date1.getDate()-date2.getDate());
          }
        }
        return compare;
      }
  
      function getFirstWeekStartDate(janFourth) {
          switch (janFourth.getDay()) {
            case 0: // Sunday
              return new Date(janFourth.getFullYear()-1, 11, 29);
            case 1: // Monday
              return janFourth;
            case 2: // Tuesday
              return new Date(janFourth.getFullYear(), 0, 3);
            case 3: // Wednesday
              return new Date(janFourth.getFullYear(), 0, 2);
            case 4: // Thursday
              return new Date(janFourth.getFullYear(), 0, 1);
            case 5: // Friday
              return new Date(janFourth.getFullYear()-1, 11, 31);
            case 6: // Saturday
              return new Date(janFourth.getFullYear()-1, 11, 30);
          }
      }
  
      function getWeekBasedYear(date) {
          var thisDate = __addDays(new Date(date.tm_year+1900, 0, 1), date.tm_yday);
  
          var janFourthThisYear = new Date(thisDate.getFullYear(), 0, 4);
          var janFourthNextYear = new Date(thisDate.getFullYear()+1, 0, 4);
  
          var firstWeekStartThisYear = getFirstWeekStartDate(janFourthThisYear);
          var firstWeekStartNextYear = getFirstWeekStartDate(janFourthNextYear);
  
          if (compareByDay(firstWeekStartThisYear, thisDate) <= 0) {
            // this date is after the start of the first week of this year
            if (compareByDay(firstWeekStartNextYear, thisDate) <= 0) {
              return thisDate.getFullYear()+1;
            } else {
              return thisDate.getFullYear();
            }
          } else {
            return thisDate.getFullYear()-1;
          }
      }
  
      var EXPANSION_RULES_2 = {
        '%a': function(date) {
          return WEEKDAYS[date.tm_wday].substring(0,3);
        },
        '%A': function(date) {
          return WEEKDAYS[date.tm_wday];
        },
        '%b': function(date) {
          return MONTHS[date.tm_mon].substring(0,3);
        },
        '%B': function(date) {
          return MONTHS[date.tm_mon];
        },
        '%C': function(date) {
          var year = date.tm_year+1900;
          return leadingNulls((year/100)|0,2);
        },
        '%d': function(date) {
          return leadingNulls(date.tm_mday, 2);
        },
        '%e': function(date) {
          return leadingSomething(date.tm_mday, 2, ' ');
        },
        '%g': function(date) {
          // %g, %G, and %V give values according to the ISO 8601:2000 standard week-based year.
          // In this system, weeks begin on a Monday and week 1 of the year is the week that includes
          // January 4th, which is also the week that includes the first Thursday of the year, and
          // is also the first week that contains at least four days in the year.
          // If the first Monday of January is the 2nd, 3rd, or 4th, the preceding days are part of
          // the last week of the preceding year; thus, for Saturday 2nd January 1999,
          // %G is replaced by 1998 and %V is replaced by 53. If December 29th, 30th,
          // or 31st is a Monday, it and any following days are part of week 1 of the following year.
          // Thus, for Tuesday 30th December 1997, %G is replaced by 1998 and %V is replaced by 01.
  
          return getWeekBasedYear(date).toString().substring(2);
        },
        '%G': function(date) {
          return getWeekBasedYear(date);
        },
        '%H': function(date) {
          return leadingNulls(date.tm_hour, 2);
        },
        '%I': function(date) {
          var twelveHour = date.tm_hour;
          if (twelveHour == 0) twelveHour = 12;
          else if (twelveHour > 12) twelveHour -= 12;
          return leadingNulls(twelveHour, 2);
        },
        '%j': function(date) {
          // Day of the year (001-366)
          return leadingNulls(date.tm_mday+__arraySum(__isLeapYear(date.tm_year+1900) ? __MONTH_DAYS_LEAP : __MONTH_DAYS_REGULAR, date.tm_mon-1), 3);
        },
        '%m': function(date) {
          return leadingNulls(date.tm_mon+1, 2);
        },
        '%M': function(date) {
          return leadingNulls(date.tm_min, 2);
        },
        '%n': function() {
          return '\n';
        },
        '%p': function(date) {
          if (date.tm_hour >= 0 && date.tm_hour < 12) {
            return 'AM';
          } else {
            return 'PM';
          }
        },
        '%S': function(date) {
          return leadingNulls(date.tm_sec, 2);
        },
        '%t': function() {
          return '\t';
        },
        '%u': function(date) {
          return date.tm_wday || 7;
        },
        '%U': function(date) {
          var days = date.tm_yday + 7 - date.tm_wday;
          return leadingNulls(Math.floor(days / 7), 2);
        },
        '%V': function(date) {
          // Replaced by the week number of the year (Monday as the first day of the week)
          // as a decimal number [01,53]. If the week containing 1 January has four
          // or more days in the new year, then it is considered week 1.
          // Otherwise, it is the last week of the previous year, and the next week is week 1.
          // Both January 4th and the first Thursday of January are always in week 1. [ tm_year, tm_wday, tm_yday]
          var val = Math.floor((date.tm_yday + 7 - (date.tm_wday + 6) % 7 ) / 7);
          // If 1 Jan is just 1-3 days past Monday, the previous week
          // is also in this year.
          if ((date.tm_wday + 371 - date.tm_yday - 2) % 7 <= 2) {
            val++;
          }
          if (!val) {
            val = 52;
            // If 31 December of prev year a Thursday, or Friday of a
            // leap year, then the prev year has 53 weeks.
            var dec31 = (date.tm_wday + 7 - date.tm_yday - 1) % 7;
            if (dec31 == 4 || (dec31 == 5 && __isLeapYear(date.tm_year%400-1))) {
              val++;
            }
          } else if (val == 53) {
            // If 1 January is not a Thursday, and not a Wednesday of a
            // leap year, then this year has only 52 weeks.
            var jan1 = (date.tm_wday + 371 - date.tm_yday) % 7;
            if (jan1 != 4 && (jan1 != 3 || !__isLeapYear(date.tm_year)))
              val = 1;
          }
          return leadingNulls(val, 2);
        },
        '%w': function(date) {
          return date.tm_wday;
        },
        '%W': function(date) {
          var days = date.tm_yday + 7 - ((date.tm_wday + 6) % 7);
          return leadingNulls(Math.floor(days / 7), 2);
        },
        '%y': function(date) {
          // Replaced by the last two digits of the year as a decimal number [00,99]. [ tm_year]
          return (date.tm_year+1900).toString().substring(2);
        },
        '%Y': function(date) {
          // Replaced by the year as a decimal number (for example, 1997). [ tm_year]
          return date.tm_year+1900;
        },
        '%z': function(date) {
          // Replaced by the offset from UTC in the ISO 8601:2000 standard format ( +hhmm or -hhmm ).
          // For example, "-0430" means 4 hours 30 minutes behind UTC (west of Greenwich).
          var off = date.tm_gmtoff;
          var ahead = off >= 0;
          off = Math.abs(off) / 60;
          // convert from minutes into hhmm format (which means 60 minutes = 100 units)
          off = (off / 60)*100 + (off % 60);
          return (ahead ? '+' : '-') + String("0000" + off).slice(-4);
        },
        '%Z': function(date) {
          return date.tm_zone;
        },
        '%%': function() {
          return '%';
        }
      };
  
      // Replace %% with a pair of NULLs (which cannot occur in a C string), then
      // re-inject them after processing.
      pattern = pattern.replace(/%%/g, '\0\0')
      for (var rule in EXPANSION_RULES_2) {
        if (pattern.includes(rule)) {
          pattern = pattern.replace(new RegExp(rule, 'g'), EXPANSION_RULES_2[rule](date));
        }
      }
      pattern = pattern.replace(/\0\0/g, '%')
  
      var bytes = intArrayFromString(pattern, false);
      if (bytes.length > maxsize) {
        return 0;
      }
  
      writeArrayToMemory(bytes, s);
      return bytes.length-1;
    }


  function runAndAbortIfError(func) {
      try {
        return func();
      } catch (e) {
        abort(e);
      }
    }
  var Asyncify = {State:{Normal:0,Unwinding:1,Rewinding:2,Disabled:3},state:0,StackSize:10000000,currData:null,handleSleepReturnValue:0,exportCallStack:[],callStackNameToId:{},callStackIdToName:{},callStackId:0,asyncPromiseHandlers:null,sleepCallbacks:[],getCallStackId:function(funcName) {
        var id = Asyncify.callStackNameToId[funcName];
        if (id === undefined) {
          id = Asyncify.callStackId++;
          Asyncify.callStackNameToId[funcName] = id;
          Asyncify.callStackIdToName[id] = funcName;
        }
        return id;
      },instrumentWasmExports:function(exports) {
        var ret = {};
        for (var x in exports) {
          (function(x) {
            var original = exports[x];
            if (typeof original == 'function') {
              ret[x] = function() {
                Asyncify.exportCallStack.push(x);
                try {
                  return original.apply(null, arguments);
                } finally {
                  if (!ABORT) {
                    var y = Asyncify.exportCallStack.pop();
                    assert(y === x);
                    Asyncify.maybeStopUnwind();
                  }
                }
              };
            } else {
              ret[x] = original;
            }
          })(x);
        }
        return ret;
      },maybeStopUnwind:function() {
        if (Asyncify.currData &&
            Asyncify.state === Asyncify.State.Unwinding &&
            Asyncify.exportCallStack.length === 0) {
          // We just finished unwinding.
          
          Asyncify.state = Asyncify.State.Normal;
          // Keep the runtime alive so that a re-wind can be done later.
          runAndAbortIfError(Module['_asyncify_stop_unwind']);
          if (typeof Fibers != 'undefined') {
            Fibers.trampoline();
          }
        }
      },whenDone:function() {
        return new Promise((resolve, reject) => {
          Asyncify.asyncPromiseHandlers = {
            resolve: resolve,
            reject: reject
          };
        });
      },allocateData:function() {
        // An asyncify data structure has three fields:
        //  0  current stack pos
        //  4  max stack pos
        //  8  id of function at bottom of the call stack (callStackIdToName[id] == name of js function)
        //
        // The Asyncify ABI only interprets the first two fields, the rest is for the runtime.
        // We also embed a stack in the same memory region here, right next to the structure.
        // This struct is also defined as asyncify_data_t in emscripten/fiber.h
        var ptr = _malloc(12 + Asyncify.StackSize);
        Asyncify.setDataHeader(ptr, ptr + 12, Asyncify.StackSize);
        Asyncify.setDataRewindFunc(ptr);
        return ptr;
      },setDataHeader:function(ptr, stack, stackSize) {
        HEAP32[((ptr)>>2)] = stack;
        HEAP32[(((ptr)+(4))>>2)] = stack + stackSize;
      },setDataRewindFunc:function(ptr) {
        var bottomOfCallStack = Asyncify.exportCallStack[0];
        var rewindId = Asyncify.getCallStackId(bottomOfCallStack);
        HEAP32[(((ptr)+(8))>>2)] = rewindId;
      },getDataRewindFunc:function(ptr) {
        var id = HEAP32[(((ptr)+(8))>>2)];
        var name = Asyncify.callStackIdToName[id];
        var func = Module['asm'][name];
        return func;
      },doRewind:function(ptr) {
        var start = Asyncify.getDataRewindFunc(ptr);
        // Once we have rewound and the stack we no longer need to artificially keep
        // the runtime alive.
        
        return start();
      },handleSleep:function(startAsync) {
        if (ABORT) return;
        if (Asyncify.state === Asyncify.State.Normal) {
          // Prepare to sleep. Call startAsync, and see what happens:
          // if the code decided to call our callback synchronously,
          // then no async operation was in fact begun, and we don't
          // need to do anything.
          var reachedCallback = false;
          var reachedAfterCallback = false;
          startAsync((handleSleepReturnValue) => {
            if (ABORT) return;
            Asyncify.handleSleepReturnValue = handleSleepReturnValue || 0;
            reachedCallback = true;
            if (!reachedAfterCallback) {
              // We are happening synchronously, so no need for async.
              return;
            }
            Asyncify.state = Asyncify.State.Rewinding;
            runAndAbortIfError(() => Module['_asyncify_start_rewind'](Asyncify.currData));
            if (typeof Browser != 'undefined' && Browser.mainLoop.func) {
              Browser.mainLoop.resume();
            }
            var asyncWasmReturnValue, isError = false;
            try {
              asyncWasmReturnValue = Asyncify.doRewind(Asyncify.currData);
            } catch (err) {
              asyncWasmReturnValue = err;
              isError = true;
            }
            // Track whether the return value was handled by any promise handlers.
            var handled = false;
            if (!Asyncify.currData) {
              // All asynchronous execution has finished.
              // `asyncWasmReturnValue` now contains the final
              // return value of the exported async WASM function.
              //
              // Note: `asyncWasmReturnValue` is distinct from
              // `Asyncify.handleSleepReturnValue`.
              // `Asyncify.handleSleepReturnValue` contains the return
              // value of the last C function to have executed
              // `Asyncify.handleSleep()`, where as `asyncWasmReturnValue`
              // contains the return value of the exported WASM function
              // that may have called C functions that
              // call `Asyncify.handleSleep()`.
              var asyncPromiseHandlers = Asyncify.asyncPromiseHandlers;
              if (asyncPromiseHandlers) {
                Asyncify.asyncPromiseHandlers = null;
                (isError ? asyncPromiseHandlers.reject : asyncPromiseHandlers.resolve)(asyncWasmReturnValue);
                handled = true;
              }
            }
            if (isError && !handled) {
              // If there was an error and it was not handled by now, we have no choice but to
              // rethrow that error into the global scope where it can be caught only by
              // `onerror` or `onunhandledpromiserejection`.
              throw asyncWasmReturnValue;
            }
          });
          reachedAfterCallback = true;
          if (!reachedCallback) {
            // A true async operation was begun; start a sleep.
            Asyncify.state = Asyncify.State.Unwinding;
            // TODO: reuse, don't alloc/free every sleep
            Asyncify.currData = Asyncify.allocateData();
            runAndAbortIfError(() => Module['_asyncify_start_unwind'](Asyncify.currData));
            if (typeof Browser != 'undefined' && Browser.mainLoop.func) {
              Browser.mainLoop.pause();
            }
          }
        } else if (Asyncify.state === Asyncify.State.Rewinding) {
          // Stop a resume.
          Asyncify.state = Asyncify.State.Normal;
          runAndAbortIfError(Module['_asyncify_stop_rewind']);
          _free(Asyncify.currData);
          Asyncify.currData = null;
          // Call all sleep callbacks now that the sleep-resume is all done.
          Asyncify.sleepCallbacks.forEach((func) => callUserCallback(func));
        } else {
          abort('invalid state: ' + Asyncify.state);
        }
        return Asyncify.handleSleepReturnValue;
      },handleAsync:function(startAsync) {
        return Asyncify.handleSleep((wakeUp) => {
          // TODO: add error handling as a second param when handleSleep implements it.
          startAsync().then(wakeUp);
        });
      }};

  var FSNode = /** @constructor */ function(parent, name, mode, rdev) {
    if (!parent) {
      parent = this;  // root node sets parent to itself
    }
    this.parent = parent;
    this.mount = parent.mount;
    this.mounted = null;
    this.id = FS.nextInode++;
    this.name = name;
    this.mode = mode;
    this.node_ops = {};
    this.stream_ops = {};
    this.rdev = rdev;
  };
  var readMode = 292/*292*/ | 73/*73*/;
  var writeMode = 146/*146*/;
  Object.defineProperties(FSNode.prototype, {
   read: {
    get: /** @this{FSNode} */function() {
     return (this.mode & readMode) === readMode;
    },
    set: /** @this{FSNode} */function(val) {
     val ? this.mode |= readMode : this.mode &= ~readMode;
    }
   },
   write: {
    get: /** @this{FSNode} */function() {
     return (this.mode & writeMode) === writeMode;
    },
    set: /** @this{FSNode} */function(val) {
     val ? this.mode |= writeMode : this.mode &= ~writeMode;
    }
   },
   isFolder: {
    get: /** @this{FSNode} */function() {
     return FS.isDir(this.mode);
    }
   },
   isDevice: {
    get: /** @this{FSNode} */function() {
     return FS.isChrdev(this.mode);
    }
   }
  });
  FS.FSNode = FSNode;
  FS.staticInit();Module["FS_createPath"] = FS.createPath;Module["FS_createDataFile"] = FS.createDataFile;Module["FS_readFile"] = FS.readFile;Module["FS_createPath"] = FS.createPath;Module["FS_createDataFile"] = FS.createDataFile;Module["FS_createPreloadedFile"] = FS.createPreloadedFile;Module["FS_unlink"] = FS.unlink;Module["FS_createLazyFile"] = FS.createLazyFile;Module["FS_createDevice"] = FS.createDevice;;
var GLctx;;
for (var i = 0; i < 32; ++i) tempFixedLengthArray.push(new Array(i));;
var miniTempWebGLFloatBuffersStorage = new Float32Array(288);
  for (/**@suppress{duplicate}*/var i = 0; i < 288; ++i) {
  miniTempWebGLFloatBuffers[i] = miniTempWebGLFloatBuffersStorage.subarray(0, i+1);
  }
  ;
var __miniTempWebGLIntBuffersStorage = new Int32Array(288);
  for (/**@suppress{duplicate}*/var i = 0; i < 288; ++i) {
  __miniTempWebGLIntBuffers[i] = __miniTempWebGLIntBuffersStorage.subarray(0, i+1);
  }
  ;
Module["requestFullscreen"] = function Module_requestFullscreen(lockPointer, resizeCanvas) { Browser.requestFullscreen(lockPointer, resizeCanvas) };
  Module["requestAnimationFrame"] = function Module_requestAnimationFrame(func) { Browser.requestAnimationFrame(func) };
  Module["setCanvasSize"] = function Module_setCanvasSize(width, height, noUpdates) { Browser.setCanvasSize(width, height, noUpdates) };
  Module["pauseMainLoop"] = function Module_pauseMainLoop() { Browser.mainLoop.pause() };
  Module["resumeMainLoop"] = function Module_resumeMainLoop() { Browser.mainLoop.resume() };
  Module["getUserMedia"] = function Module_getUserMedia() { Browser.getUserMedia() };
  Module["createContext"] = function Module_createContext(canvas, useWebGL, setInModule, webGLContextAttributes) { return Browser.createContext(canvas, useWebGL, setInModule, webGLContextAttributes) };
  var preloadedImages = {};
  var preloadedAudios = {};
  ;

  let __dotnet_replacement_PThread = false ? {} : undefined;
  if (false) {
      __dotnet_replacement_PThread.loadWasmModuleToWorker = PThread.loadWasmModuleToWorker;
      __dotnet_replacement_PThread.threadInitTLS = PThread.threadInitTLS;
      __dotnet_replacement_PThread.allocateUnusedWorker = PThread.allocateUnusedWorker;
  }
  let __dotnet_replacements = {scriptUrl: import.meta.url, fetch: globalThis.fetch, require, updateGlobalBufferAndViews, pthreadReplacements: __dotnet_replacement_PThread};
  if (ENVIRONMENT_IS_NODE) {
      __dotnet_replacements.requirePromise = import(/* webpackIgnore: true */'module').then(mod => mod.createRequire(import.meta.url));
  }
  let __dotnet_exportedAPI = __dotnet_runtime.__initializeImportsAndExports(
      { isGlobal:false, isNode:ENVIRONMENT_IS_NODE, isWorker:ENVIRONMENT_IS_WORKER, isShell:ENVIRONMENT_IS_SHELL, isWeb:ENVIRONMENT_IS_WEB, isPThread:false, quit_, ExitStatus, requirePromise:__dotnet_replacements.requirePromise },
      { mono:MONO, binding:BINDING, internal:INTERNAL, module:Module, marshaled_imports: IMPORTS },
      __dotnet_replacements, __callbackAPI);
  updateGlobalBufferAndViews = __dotnet_replacements.updateGlobalBufferAndViews;
  var fetch = __dotnet_replacements.fetch;
  _scriptDir = __dirname = scriptDirectory = __dotnet_replacements.scriptDirectory;
  if (ENVIRONMENT_IS_NODE) {
      __dotnet_replacements.requirePromise.then(someRequire => {
          require = someRequire;
      });
  }
  var noExitRuntime = __dotnet_replacements.noExitRuntime;
  if (false) {
      PThread.loadWasmModuleToWorker = __dotnet_replacements.pthreadReplacements.loadWasmModuleToWorker;
      PThread.threadInitTLS = __dotnet_replacements.pthreadReplacements.threadInitTLS;
      PThread.allocateUnusedWorker = __dotnet_replacements.pthreadReplacements.allocateUnusedWorker;
  }
  ;
var ASSERTIONS = false;



/** @type {function(string, boolean=, number=)} */
function intArrayFromString(stringy, dontAddNull, length) {
  var len = length > 0 ? length : lengthBytesUTF8(stringy)+1;
  var u8array = new Array(len);
  var numBytesWritten = stringToUTF8Array(stringy, u8array, 0, u8array.length);
  if (dontAddNull) u8array.length = numBytesWritten;
  return u8array;
}

function intArrayToString(array) {
  var ret = [];
  for (var i = 0; i < array.length; i++) {
    var chr = array[i];
    if (chr > 0xFF) {
      if (ASSERTIONS) {
        assert(false, 'Character code ' + chr + ' (' + String.fromCharCode(chr) + ')  at offset ' + i + ' not in 0x00-0xFF.');
      }
      chr &= 0xFF;
    }
    ret.push(String.fromCharCode(chr));
  }
  return ret.join('');
}


// Copied from https://github.com/strophe/strophejs/blob/e06d027/src/polyfills.js#L149

// This code was written by Tyler Akins and has been placed in the
// public domain.  It would be nice if you left this header intact.
// Base64 code from Tyler Akins -- http://rumkin.com

/**
 * Decodes a base64 string.
 * @param {string} input The string to decode.
 */
var decodeBase64 = typeof atob == 'function' ? atob : function (input) {
  var keyStr = 'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/=';

  var output = '';
  var chr1, chr2, chr3;
  var enc1, enc2, enc3, enc4;
  var i = 0;
  // remove all characters that are not A-Z, a-z, 0-9, +, /, or =
  input = input.replace(/[^A-Za-z0-9\+\/\=]/g, '');
  do {
    enc1 = keyStr.indexOf(input.charAt(i++));
    enc2 = keyStr.indexOf(input.charAt(i++));
    enc3 = keyStr.indexOf(input.charAt(i++));
    enc4 = keyStr.indexOf(input.charAt(i++));

    chr1 = (enc1 << 2) | (enc2 >> 4);
    chr2 = ((enc2 & 15) << 4) | (enc3 >> 2);
    chr3 = ((enc3 & 3) << 6) | enc4;

    output = output + String.fromCharCode(chr1);

    if (enc3 !== 64) {
      output = output + String.fromCharCode(chr2);
    }
    if (enc4 !== 64) {
      output = output + String.fromCharCode(chr3);
    }
  } while (i < input.length);
  return output;
};

// Converts a string of base64 into a byte array.
// Throws error on invalid input.
function intArrayFromBase64(s) {
  if (typeof ENVIRONMENT_IS_NODE == 'boolean' && ENVIRONMENT_IS_NODE) {
    var buf = Buffer.from(s, 'base64');
    return new Uint8Array(buf['buffer'], buf['byteOffset'], buf['byteLength']);
  }

  try {
    var decoded = decodeBase64(s);
    var bytes = new Uint8Array(decoded.length);
    for (var i = 0 ; i < decoded.length ; ++i) {
      bytes[i] = decoded.charCodeAt(i);
    }
    return bytes;
  } catch (_) {
    throw new Error('Converting base64 string to bytes failed.');
  }
}

// If filename is a base64 data URI, parses and returns data (Buffer on node,
// Uint8Array otherwise). If filename is not a base64 data URI, returns undefined.
function tryParseAsDataURI(filename) {
  if (!isDataURI(filename)) {
    return;
  }

  return intArrayFromBase64(filename.slice(dataURIPrefix.length));
}


var asmLibraryArg = {
  "GetCanvasHeight": GetCanvasHeight,
  "GetCanvasWidth": GetCanvasWidth,
  "__assert_fail": ___assert_fail,
  "__cxa_allocate_exception": ___cxa_allocate_exception,
  "__cxa_begin_catch": ___cxa_begin_catch,
  "__cxa_end_catch": ___cxa_end_catch,
  "__cxa_find_matching_catch_3": ___cxa_find_matching_catch_3,
  "__cxa_throw": ___cxa_throw,
  "__resumeException": ___resumeException,
  "__syscall_connect": ___syscall_connect,
  "__syscall_faccessat": ___syscall_faccessat,
  "__syscall_fadvise64": ___syscall_fadvise64,
  "__syscall_fcntl64": ___syscall_fcntl64,
  "__syscall_fstat64": ___syscall_fstat64,
  "__syscall_fstatfs64": ___syscall_fstatfs64,
  "__syscall_ftruncate64": ___syscall_ftruncate64,
  "__syscall_getcwd": ___syscall_getcwd,
  "__syscall_getdents64": ___syscall_getdents64,
  "__syscall_ioctl": ___syscall_ioctl,
  "__syscall_lstat64": ___syscall_lstat64,
  "__syscall_newfstatat": ___syscall_newfstatat,
  "__syscall_openat": ___syscall_openat,
  "__syscall_readlinkat": ___syscall_readlinkat,
  "__syscall_recvfrom": ___syscall_recvfrom,
  "__syscall_sendto": ___syscall_sendto,
  "__syscall_socket": ___syscall_socket,
  "__syscall_stat64": ___syscall_stat64,
  "__syscall_unlinkat": ___syscall_unlinkat,
  "_emscripten_date_now": __emscripten_date_now,
  "_emscripten_get_now_is_monotonic": __emscripten_get_now_is_monotonic,
  "_gmtime_js": __gmtime_js,
  "_localtime_js": __localtime_js,
  "_mmap_js": __mmap_js,
  "_munmap_js": __munmap_js,
  "_tzset_js": __tzset_js,
  "abort": _abort,
  "dotnet_browser_entropy": _dotnet_browser_entropy,
  "emscripten_exit_pointerlock": _emscripten_exit_pointerlock,
  "emscripten_get_element_css_size": _emscripten_get_element_css_size,
  "emscripten_get_gamepad_status": _emscripten_get_gamepad_status,
  "emscripten_get_heap_max": _emscripten_get_heap_max,
  "emscripten_get_now": _emscripten_get_now,
  "emscripten_get_now_res": _emscripten_get_now_res,
  "emscripten_get_num_gamepads": _emscripten_get_num_gamepads,
  "emscripten_glActiveTexture": _emscripten_glActiveTexture,
  "emscripten_glAttachShader": _emscripten_glAttachShader,
  "emscripten_glBeginQueryEXT": _emscripten_glBeginQueryEXT,
  "emscripten_glBindAttribLocation": _emscripten_glBindAttribLocation,
  "emscripten_glBindBuffer": _emscripten_glBindBuffer,
  "emscripten_glBindFramebuffer": _emscripten_glBindFramebuffer,
  "emscripten_glBindRenderbuffer": _emscripten_glBindRenderbuffer,
  "emscripten_glBindTexture": _emscripten_glBindTexture,
  "emscripten_glBindVertexArrayOES": _emscripten_glBindVertexArrayOES,
  "emscripten_glBlendColor": _emscripten_glBlendColor,
  "emscripten_glBlendEquation": _emscripten_glBlendEquation,
  "emscripten_glBlendEquationSeparate": _emscripten_glBlendEquationSeparate,
  "emscripten_glBlendFunc": _emscripten_glBlendFunc,
  "emscripten_glBlendFuncSeparate": _emscripten_glBlendFuncSeparate,
  "emscripten_glBufferData": _emscripten_glBufferData,
  "emscripten_glBufferSubData": _emscripten_glBufferSubData,
  "emscripten_glCheckFramebufferStatus": _emscripten_glCheckFramebufferStatus,
  "emscripten_glClear": _emscripten_glClear,
  "emscripten_glClearColor": _emscripten_glClearColor,
  "emscripten_glClearDepthf": _emscripten_glClearDepthf,
  "emscripten_glClearStencil": _emscripten_glClearStencil,
  "emscripten_glColorMask": _emscripten_glColorMask,
  "emscripten_glCompileShader": _emscripten_glCompileShader,
  "emscripten_glCompressedTexImage2D": _emscripten_glCompressedTexImage2D,
  "emscripten_glCompressedTexSubImage2D": _emscripten_glCompressedTexSubImage2D,
  "emscripten_glCopyTexImage2D": _emscripten_glCopyTexImage2D,
  "emscripten_glCopyTexSubImage2D": _emscripten_glCopyTexSubImage2D,
  "emscripten_glCreateProgram": _emscripten_glCreateProgram,
  "emscripten_glCreateShader": _emscripten_glCreateShader,
  "emscripten_glCullFace": _emscripten_glCullFace,
  "emscripten_glDeleteBuffers": _emscripten_glDeleteBuffers,
  "emscripten_glDeleteFramebuffers": _emscripten_glDeleteFramebuffers,
  "emscripten_glDeleteProgram": _emscripten_glDeleteProgram,
  "emscripten_glDeleteQueriesEXT": _emscripten_glDeleteQueriesEXT,
  "emscripten_glDeleteRenderbuffers": _emscripten_glDeleteRenderbuffers,
  "emscripten_glDeleteShader": _emscripten_glDeleteShader,
  "emscripten_glDeleteTextures": _emscripten_glDeleteTextures,
  "emscripten_glDeleteVertexArraysOES": _emscripten_glDeleteVertexArraysOES,
  "emscripten_glDepthFunc": _emscripten_glDepthFunc,
  "emscripten_glDepthMask": _emscripten_glDepthMask,
  "emscripten_glDepthRangef": _emscripten_glDepthRangef,
  "emscripten_glDetachShader": _emscripten_glDetachShader,
  "emscripten_glDisable": _emscripten_glDisable,
  "emscripten_glDisableVertexAttribArray": _emscripten_glDisableVertexAttribArray,
  "emscripten_glDrawArrays": _emscripten_glDrawArrays,
  "emscripten_glDrawArraysInstancedANGLE": _emscripten_glDrawArraysInstancedANGLE,
  "emscripten_glDrawBuffersWEBGL": _emscripten_glDrawBuffersWEBGL,
  "emscripten_glDrawElements": _emscripten_glDrawElements,
  "emscripten_glDrawElementsInstancedANGLE": _emscripten_glDrawElementsInstancedANGLE,
  "emscripten_glEnable": _emscripten_glEnable,
  "emscripten_glEnableVertexAttribArray": _emscripten_glEnableVertexAttribArray,
  "emscripten_glEndQueryEXT": _emscripten_glEndQueryEXT,
  "emscripten_glFinish": _emscripten_glFinish,
  "emscripten_glFlush": _emscripten_glFlush,
  "emscripten_glFramebufferRenderbuffer": _emscripten_glFramebufferRenderbuffer,
  "emscripten_glFramebufferTexture2D": _emscripten_glFramebufferTexture2D,
  "emscripten_glFrontFace": _emscripten_glFrontFace,
  "emscripten_glGenBuffers": _emscripten_glGenBuffers,
  "emscripten_glGenFramebuffers": _emscripten_glGenFramebuffers,
  "emscripten_glGenQueriesEXT": _emscripten_glGenQueriesEXT,
  "emscripten_glGenRenderbuffers": _emscripten_glGenRenderbuffers,
  "emscripten_glGenTextures": _emscripten_glGenTextures,
  "emscripten_glGenVertexArraysOES": _emscripten_glGenVertexArraysOES,
  "emscripten_glGenerateMipmap": _emscripten_glGenerateMipmap,
  "emscripten_glGetActiveAttrib": _emscripten_glGetActiveAttrib,
  "emscripten_glGetActiveUniform": _emscripten_glGetActiveUniform,
  "emscripten_glGetAttachedShaders": _emscripten_glGetAttachedShaders,
  "emscripten_glGetAttribLocation": _emscripten_glGetAttribLocation,
  "emscripten_glGetBooleanv": _emscripten_glGetBooleanv,
  "emscripten_glGetBufferParameteriv": _emscripten_glGetBufferParameteriv,
  "emscripten_glGetError": _emscripten_glGetError,
  "emscripten_glGetFloatv": _emscripten_glGetFloatv,
  "emscripten_glGetFramebufferAttachmentParameteriv": _emscripten_glGetFramebufferAttachmentParameteriv,
  "emscripten_glGetIntegerv": _emscripten_glGetIntegerv,
  "emscripten_glGetProgramInfoLog": _emscripten_glGetProgramInfoLog,
  "emscripten_glGetProgramiv": _emscripten_glGetProgramiv,
  "emscripten_glGetQueryObjecti64vEXT": _emscripten_glGetQueryObjecti64vEXT,
  "emscripten_glGetQueryObjectivEXT": _emscripten_glGetQueryObjectivEXT,
  "emscripten_glGetQueryObjectui64vEXT": _emscripten_glGetQueryObjectui64vEXT,
  "emscripten_glGetQueryObjectuivEXT": _emscripten_glGetQueryObjectuivEXT,
  "emscripten_glGetQueryivEXT": _emscripten_glGetQueryivEXT,
  "emscripten_glGetRenderbufferParameteriv": _emscripten_glGetRenderbufferParameteriv,
  "emscripten_glGetShaderInfoLog": _emscripten_glGetShaderInfoLog,
  "emscripten_glGetShaderPrecisionFormat": _emscripten_glGetShaderPrecisionFormat,
  "emscripten_glGetShaderSource": _emscripten_glGetShaderSource,
  "emscripten_glGetShaderiv": _emscripten_glGetShaderiv,
  "emscripten_glGetString": _emscripten_glGetString,
  "emscripten_glGetTexParameterfv": _emscripten_glGetTexParameterfv,
  "emscripten_glGetTexParameteriv": _emscripten_glGetTexParameteriv,
  "emscripten_glGetUniformLocation": _emscripten_glGetUniformLocation,
  "emscripten_glGetUniformfv": _emscripten_glGetUniformfv,
  "emscripten_glGetUniformiv": _emscripten_glGetUniformiv,
  "emscripten_glGetVertexAttribPointerv": _emscripten_glGetVertexAttribPointerv,
  "emscripten_glGetVertexAttribfv": _emscripten_glGetVertexAttribfv,
  "emscripten_glGetVertexAttribiv": _emscripten_glGetVertexAttribiv,
  "emscripten_glHint": _emscripten_glHint,
  "emscripten_glIsBuffer": _emscripten_glIsBuffer,
  "emscripten_glIsEnabled": _emscripten_glIsEnabled,
  "emscripten_glIsFramebuffer": _emscripten_glIsFramebuffer,
  "emscripten_glIsProgram": _emscripten_glIsProgram,
  "emscripten_glIsQueryEXT": _emscripten_glIsQueryEXT,
  "emscripten_glIsRenderbuffer": _emscripten_glIsRenderbuffer,
  "emscripten_glIsShader": _emscripten_glIsShader,
  "emscripten_glIsTexture": _emscripten_glIsTexture,
  "emscripten_glIsVertexArrayOES": _emscripten_glIsVertexArrayOES,
  "emscripten_glLineWidth": _emscripten_glLineWidth,
  "emscripten_glLinkProgram": _emscripten_glLinkProgram,
  "emscripten_glPixelStorei": _emscripten_glPixelStorei,
  "emscripten_glPolygonOffset": _emscripten_glPolygonOffset,
  "emscripten_glQueryCounterEXT": _emscripten_glQueryCounterEXT,
  "emscripten_glReadPixels": _emscripten_glReadPixels,
  "emscripten_glReleaseShaderCompiler": _emscripten_glReleaseShaderCompiler,
  "emscripten_glRenderbufferStorage": _emscripten_glRenderbufferStorage,
  "emscripten_glSampleCoverage": _emscripten_glSampleCoverage,
  "emscripten_glScissor": _emscripten_glScissor,
  "emscripten_glShaderBinary": _emscripten_glShaderBinary,
  "emscripten_glShaderSource": _emscripten_glShaderSource,
  "emscripten_glStencilFunc": _emscripten_glStencilFunc,
  "emscripten_glStencilFuncSeparate": _emscripten_glStencilFuncSeparate,
  "emscripten_glStencilMask": _emscripten_glStencilMask,
  "emscripten_glStencilMaskSeparate": _emscripten_glStencilMaskSeparate,
  "emscripten_glStencilOp": _emscripten_glStencilOp,
  "emscripten_glStencilOpSeparate": _emscripten_glStencilOpSeparate,
  "emscripten_glTexImage2D": _emscripten_glTexImage2D,
  "emscripten_glTexParameterf": _emscripten_glTexParameterf,
  "emscripten_glTexParameterfv": _emscripten_glTexParameterfv,
  "emscripten_glTexParameteri": _emscripten_glTexParameteri,
  "emscripten_glTexParameteriv": _emscripten_glTexParameteriv,
  "emscripten_glTexSubImage2D": _emscripten_glTexSubImage2D,
  "emscripten_glUniform1f": _emscripten_glUniform1f,
  "emscripten_glUniform1fv": _emscripten_glUniform1fv,
  "emscripten_glUniform1i": _emscripten_glUniform1i,
  "emscripten_glUniform1iv": _emscripten_glUniform1iv,
  "emscripten_glUniform2f": _emscripten_glUniform2f,
  "emscripten_glUniform2fv": _emscripten_glUniform2fv,
  "emscripten_glUniform2i": _emscripten_glUniform2i,
  "emscripten_glUniform2iv": _emscripten_glUniform2iv,
  "emscripten_glUniform3f": _emscripten_glUniform3f,
  "emscripten_glUniform3fv": _emscripten_glUniform3fv,
  "emscripten_glUniform3i": _emscripten_glUniform3i,
  "emscripten_glUniform3iv": _emscripten_glUniform3iv,
  "emscripten_glUniform4f": _emscripten_glUniform4f,
  "emscripten_glUniform4fv": _emscripten_glUniform4fv,
  "emscripten_glUniform4i": _emscripten_glUniform4i,
  "emscripten_glUniform4iv": _emscripten_glUniform4iv,
  "emscripten_glUniformMatrix2fv": _emscripten_glUniformMatrix2fv,
  "emscripten_glUniformMatrix3fv": _emscripten_glUniformMatrix3fv,
  "emscripten_glUniformMatrix4fv": _emscripten_glUniformMatrix4fv,
  "emscripten_glUseProgram": _emscripten_glUseProgram,
  "emscripten_glValidateProgram": _emscripten_glValidateProgram,
  "emscripten_glVertexAttrib1f": _emscripten_glVertexAttrib1f,
  "emscripten_glVertexAttrib1fv": _emscripten_glVertexAttrib1fv,
  "emscripten_glVertexAttrib2f": _emscripten_glVertexAttrib2f,
  "emscripten_glVertexAttrib2fv": _emscripten_glVertexAttrib2fv,
  "emscripten_glVertexAttrib3f": _emscripten_glVertexAttrib3f,
  "emscripten_glVertexAttrib3fv": _emscripten_glVertexAttrib3fv,
  "emscripten_glVertexAttrib4f": _emscripten_glVertexAttrib4f,
  "emscripten_glVertexAttrib4fv": _emscripten_glVertexAttrib4fv,
  "emscripten_glVertexAttribDivisorANGLE": _emscripten_glVertexAttribDivisorANGLE,
  "emscripten_glVertexAttribPointer": _emscripten_glVertexAttribPointer,
  "emscripten_glViewport": _emscripten_glViewport,
  "emscripten_memcpy_big": _emscripten_memcpy_big,
  "emscripten_request_pointerlock": _emscripten_request_pointerlock,
  "emscripten_resize_heap": _emscripten_resize_heap,
  "emscripten_run_script": _emscripten_run_script,
  "emscripten_sample_gamepad_data": _emscripten_sample_gamepad_data,
  "emscripten_set_click_callback_on_thread": _emscripten_set_click_callback_on_thread,
  "emscripten_set_fullscreenchange_callback_on_thread": _emscripten_set_fullscreenchange_callback_on_thread,
  "emscripten_set_gamepadconnected_callback_on_thread": _emscripten_set_gamepadconnected_callback_on_thread,
  "emscripten_set_gamepaddisconnected_callback_on_thread": _emscripten_set_gamepaddisconnected_callback_on_thread,
  "emscripten_set_touchcancel_callback_on_thread": _emscripten_set_touchcancel_callback_on_thread,
  "emscripten_set_touchend_callback_on_thread": _emscripten_set_touchend_callback_on_thread,
  "emscripten_set_touchmove_callback_on_thread": _emscripten_set_touchmove_callback_on_thread,
  "emscripten_set_touchstart_callback_on_thread": _emscripten_set_touchstart_callback_on_thread,
  "emscripten_sleep": _emscripten_sleep,
  "environ_get": _environ_get,
  "environ_sizes_get": _environ_sizes_get,
  "exit": _exit,
  "fd_close": _fd_close,
  "fd_pread": _fd_pread,
  "fd_pwrite": _fd_pwrite,
  "fd_read": _fd_read,
  "fd_seek": _fd_seek,
  "fd_write": _fd_write,
  "getTempRet0": _getTempRet0,
  "glActiveTexture": _glActiveTexture,
  "glAttachShader": _glAttachShader,
  "glBindAttribLocation": _glBindAttribLocation,
  "glBindBuffer": _glBindBuffer,
  "glBindTexture": _glBindTexture,
  "glBlendFunc": _glBlendFunc,
  "glBufferData": _glBufferData,
  "glBufferSubData": _glBufferSubData,
  "glClear": _glClear,
  "glClearColor": _glClearColor,
  "glClearDepthf": _glClearDepthf,
  "glCompileShader": _glCompileShader,
  "glCompressedTexImage2D": _glCompressedTexImage2D,
  "glCreateProgram": _glCreateProgram,
  "glCreateShader": _glCreateShader,
  "glCullFace": _glCullFace,
  "glDeleteBuffers": _glDeleteBuffers,
  "glDeleteProgram": _glDeleteProgram,
  "glDeleteShader": _glDeleteShader,
  "glDeleteTextures": _glDeleteTextures,
  "glDepthFunc": _glDepthFunc,
  "glDetachShader": _glDetachShader,
  "glDisable": _glDisable,
  "glDisableVertexAttribArray": _glDisableVertexAttribArray,
  "glDrawArrays": _glDrawArrays,
  "glDrawElements": _glDrawElements,
  "glEnable": _glEnable,
  "glEnableVertexAttribArray": _glEnableVertexAttribArray,
  "glFrontFace": _glFrontFace,
  "glGenBuffers": _glGenBuffers,
  "glGenTextures": _glGenTextures,
  "glGetAttribLocation": _glGetAttribLocation,
  "glGetFloatv": _glGetFloatv,
  "glGetProgramInfoLog": _glGetProgramInfoLog,
  "glGetProgramiv": _glGetProgramiv,
  "glGetShaderInfoLog": _glGetShaderInfoLog,
  "glGetShaderiv": _glGetShaderiv,
  "glGetString": _glGetString,
  "glGetUniformLocation": _glGetUniformLocation,
  "glLinkProgram": _glLinkProgram,
  "glPixelStorei": _glPixelStorei,
  "glReadPixels": _glReadPixels,
  "glShaderSource": _glShaderSource,
  "glTexImage2D": _glTexImage2D,
  "glTexParameterf": _glTexParameterf,
  "glTexParameteri": _glTexParameteri,
  "glUniform1fv": _glUniform1fv,
  "glUniform1i": _glUniform1i,
  "glUniform1iv": _glUniform1iv,
  "glUniform2fv": _glUniform2fv,
  "glUniform2iv": _glUniform2iv,
  "glUniform3fv": _glUniform3fv,
  "glUniform3iv": _glUniform3iv,
  "glUniform4f": _glUniform4f,
  "glUniform4fv": _glUniform4fv,
  "glUniform4iv": _glUniform4iv,
  "glUniformMatrix4fv": _glUniformMatrix4fv,
  "glUseProgram": _glUseProgram,
  "glVertexAttrib1fv": _glVertexAttrib1fv,
  "glVertexAttrib2fv": _glVertexAttrib2fv,
  "glVertexAttrib3fv": _glVertexAttrib3fv,
  "glVertexAttrib4fv": _glVertexAttrib4fv,
  "glVertexAttribPointer": _glVertexAttribPointer,
  "glViewport": _glViewport,
  "glfwCreateWindow": _glfwCreateWindow,
  "glfwDefaultWindowHints": _glfwDefaultWindowHints,
  "glfwDestroyWindow": _glfwDestroyWindow,
  "glfwGetPrimaryMonitor": _glfwGetPrimaryMonitor,
  "glfwGetTime": _glfwGetTime,
  "glfwGetVideoModes": _glfwGetVideoModes,
  "glfwInit": _glfwInit,
  "glfwMakeContextCurrent": _glfwMakeContextCurrent,
  "glfwSetCharCallback": _glfwSetCharCallback,
  "glfwSetCursorEnterCallback": _glfwSetCursorEnterCallback,
  "glfwSetCursorPosCallback": _glfwSetCursorPosCallback,
  "glfwSetDropCallback": _glfwSetDropCallback,
  "glfwSetErrorCallback": _glfwSetErrorCallback,
  "glfwSetKeyCallback": _glfwSetKeyCallback,
  "glfwSetMouseButtonCallback": _glfwSetMouseButtonCallback,
  "glfwSetScrollCallback": _glfwSetScrollCallback,
  "glfwSetWindowFocusCallback": _glfwSetWindowFocusCallback,
  "glfwSetWindowIconifyCallback": _glfwSetWindowIconifyCallback,
  "glfwSetWindowShouldClose": _glfwSetWindowShouldClose,
  "glfwSetWindowSizeCallback": _glfwSetWindowSizeCallback,
  "glfwSwapBuffers": _glfwSwapBuffers,
  "glfwSwapInterval": _glfwSwapInterval,
  "glfwTerminate": _glfwTerminate,
  "glfwWindowHint": _glfwWindowHint,
  "invoke_i": invoke_i,
  "invoke_ii": invoke_ii,
  "invoke_iii": invoke_iii,
  "invoke_iiii": invoke_iiii,
  "invoke_iiiii": invoke_iiiii,
  "invoke_iiiiii": invoke_iiiiii,
  "invoke_iiiiiii": invoke_iiiiiii,
  "invoke_iiiiiiii": invoke_iiiiiiii,
  "invoke_iiiiiiiii": invoke_iiiiiiiii,
  "invoke_iiiiiiijiii": invoke_iiiiiiijiii,
  "invoke_iiiiiiji": invoke_iiiiiiji,
  "invoke_iiiiiji": invoke_iiiiiji,
  "invoke_iiiji": invoke_iiiji,
  "invoke_jii": invoke_jii,
  "invoke_v": invoke_v,
  "invoke_vi": invoke_vi,
  "invoke_vii": invoke_vii,
  "invoke_viii": invoke_viii,
  "invoke_viiii": invoke_viiii,
  "invoke_viiiii": invoke_viiiii,
  "invoke_viiiiii": invoke_viiiiii,
  "invoke_viiiiiii": invoke_viiiiiii,
  "invoke_viiiiiiii": invoke_viiiiiiii,
  "invoke_viiiiiiiii": invoke_viiiiiiiii,
  "invoke_viiiiiiiiii": invoke_viiiiiiiiii,
  "invoke_viiiiiiiiiii": invoke_viiiiiiiiiii,
  "invoke_viiiiiiiiiiii": invoke_viiiiiiiiiiii,
  "invoke_viiiiiiiiiiiii": invoke_viiiiiiiiiiiii,
  "invoke_viiiiiiiiiiiiii": invoke_viiiiiiiiiiiiii,
  "invoke_viiiiiiiiiiiiiii": invoke_viiiiiiiiiiiiiii,
  "invoke_viiiiiiiiiiiiiiii": invoke_viiiiiiiiiiiiiiii,
  "invoke_viiiiiiiiiiiiiiiii": invoke_viiiiiiiiiiiiiiiii,
  "invoke_viiiiiiiiiiiiiiiiii": invoke_viiiiiiiiiiiiiiiiii,
  "invoke_viiiiiiiiiiiiiiiiiii": invoke_viiiiiiiiiiiiiiiiiii,
  "invoke_viiiiiiiiiiiiiiiiiiii": invoke_viiiiiiiiiiiiiiiiiiii,
  "invoke_viiiiiiiiiiiiiiiiiiiii": invoke_viiiiiiiiiiiiiiiiiiiii,
  "invoke_viiiiiiiiiiiiiiiiiiiiii": invoke_viiiiiiiiiiiiiiiiiiiiii,
  "invoke_viiiiiiiiiiiiiiiiiiiiiii": invoke_viiiiiiiiiiiiiiiiiiiiiii,
  "invoke_viiiiiiiiiiiiiiiiiiiiiiii": invoke_viiiiiiiiiiiiiiiiiiiiiiii,
  "invoke_viiiiiiiiiiiiiiiiiiiiiiiii": invoke_viiiiiiiiiiiiiiiiiiiiiiiii,
  "invoke_viiiiiiiiiiiiiiiiiiiiiiiiii": invoke_viiiiiiiiiiiiiiiiiiiiiiiiii,
  "invoke_viiiiiiiiiiiiiiiiiiiiiiiiiii": invoke_viiiiiiiiiiiiiiiiiiiiiiiiiii,
  "invoke_viiiiiiiiiiiiiiiiiiiiiiiiiiii": invoke_viiiiiiiiiiiiiiiiiiiiiiiiiiii,
  "invoke_viiiiiiiiiiiiiiiiiiiiiiiiiiiii": invoke_viiiiiiiiiiiiiiiiiiiiiiiiiiiii,
  "invoke_viiiiiiiiiiiiiiiiiiiiiiiiiiiiii": invoke_viiiiiiiiiiiiiiiiiiiiiiiiiiiiii,
  "invoke_viiiiiiiiiiiiiiiiiiiiiiiiiiiiiii": invoke_viiiiiiiiiiiiiiiiiiiiiiiiiiiiiii,
  "invoke_viiiiiiiiiiiiiiiiiiiiiiiiiiiiiiii": invoke_viiiiiiiiiiiiiiiiiiiiiiiiiiiiiiii,
  "invoke_viiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiii": invoke_viiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiii,
  "invoke_viiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiii": invoke_viiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiii,
  "invoke_viiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiii": invoke_viiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiii,
  "invoke_viiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiii": invoke_viiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiii,
  "invoke_viiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiii": invoke_viiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiii,
  "invoke_viiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiii": invoke_viiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiii,
  "invoke_viiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiii": invoke_viiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiii,
  "invoke_viiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiii": invoke_viiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiii,
  "invoke_viiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiii": invoke_viiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiii,
  "invoke_viiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiii": invoke_viiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiii,
  "invoke_viji": invoke_viji,
  "llvm_eh_typeid_for": _llvm_eh_typeid_for,
  "mono_set_timeout": _mono_set_timeout,
  "mono_wasm_add_dbg_command_received": _mono_wasm_add_dbg_command_received,
  "mono_wasm_asm_loaded": _mono_wasm_asm_loaded,
  "mono_wasm_bind_cs_function": _mono_wasm_bind_cs_function,
  "mono_wasm_bind_js_function": _mono_wasm_bind_js_function,
  "mono_wasm_create_cs_owned_object_ref": _mono_wasm_create_cs_owned_object_ref,
  "mono_wasm_debugger_log": _mono_wasm_debugger_log,
  "mono_wasm_fire_debugger_agent_message": _mono_wasm_fire_debugger_agent_message,
  "mono_wasm_get_by_index_ref": _mono_wasm_get_by_index_ref,
  "mono_wasm_get_global_object_ref": _mono_wasm_get_global_object_ref,
  "mono_wasm_get_object_property_ref": _mono_wasm_get_object_property_ref,
  "mono_wasm_invoke_bound_function": _mono_wasm_invoke_bound_function,
  "mono_wasm_invoke_js_blazor": _mono_wasm_invoke_js_blazor,
  "mono_wasm_invoke_js_with_args_ref": _mono_wasm_invoke_js_with_args_ref,
  "mono_wasm_marshal_promise": _mono_wasm_marshal_promise,
  "mono_wasm_release_cs_owned_object": _mono_wasm_release_cs_owned_object,
  "mono_wasm_set_by_index_ref": _mono_wasm_set_by_index_ref,
  "mono_wasm_set_entrypoint_breakpoint": _mono_wasm_set_entrypoint_breakpoint,
  "mono_wasm_set_object_property_ref": _mono_wasm_set_object_property_ref,
  "mono_wasm_trace_logger": _mono_wasm_trace_logger,
  "mono_wasm_typed_array_from_ref": _mono_wasm_typed_array_from_ref,
  "mono_wasm_typed_array_to_array_ref": _mono_wasm_typed_array_to_array_ref,
  "schedule_background_exec": _schedule_background_exec,
  "setTempRet0": _setTempRet0,
  "strftime": _strftime
};
var asm = createWasm();
/** @type {function(...*):?} */
var ___wasm_call_ctors = Module["___wasm_call_ctors"] = function() {
  return (___wasm_call_ctors = Module["___wasm_call_ctors"] = Module["asm"]["__wasm_call_ctors"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var _memset = Module["_memset"] = function() {
  return (_memset = Module["_memset"] = Module["asm"]["memset"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var _mono_aot_Jitter2_get_method = Module["_mono_aot_Jitter2_get_method"] = function() {
  return (_mono_aot_Jitter2_get_method = Module["_mono_aot_Jitter2_get_method"] = Module["asm"]["mono_aot_Jitter2_get_method"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var _mono_aot_Raylib_cs_get_method = Module["_mono_aot_Raylib_cs_get_method"] = function() {
  return (_mono_aot_Raylib_cs_get_method = Module["_mono_aot_Raylib_cs_get_method"] = Module["asm"]["mono_aot_Raylib_cs_get_method"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var _mono_aot_System_Collections_get_method = Module["_mono_aot_System_Collections_get_method"] = function() {
  return (_mono_aot_System_Collections_get_method = Module["_mono_aot_System_Collections_get_method"] = Module["asm"]["mono_aot_System_Collections_get_method"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var _mono_aot_System_Diagnostics_TraceSource_get_method = Module["_mono_aot_System_Diagnostics_TraceSource_get_method"] = function() {
  return (_mono_aot_System_Diagnostics_TraceSource_get_method = Module["_mono_aot_System_Diagnostics_TraceSource_get_method"] = Module["asm"]["mono_aot_System_Diagnostics_TraceSource_get_method"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var _mono_aot_corlib_get_method = Module["_mono_aot_corlib_get_method"] = function() {
  return (_mono_aot_corlib_get_method = Module["_mono_aot_corlib_get_method"] = Module["asm"]["mono_aot_corlib_get_method"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var _mono_aot_System_Private_Uri_get_method = Module["_mono_aot_System_Private_Uri_get_method"] = function() {
  return (_mono_aot_System_Private_Uri_get_method = Module["_mono_aot_System_Private_Uri_get_method"] = Module["asm"]["mono_aot_System_Private_Uri_get_method"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var _mono_aot_System_Runtime_InteropServices_JavaScript_get_method = Module["_mono_aot_System_Runtime_InteropServices_JavaScript_get_method"] = function() {
  return (_mono_aot_System_Runtime_InteropServices_JavaScript_get_method = Module["_mono_aot_System_Runtime_InteropServices_JavaScript_get_method"] = Module["asm"]["mono_aot_System_Runtime_InteropServices_JavaScript_get_method"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var _mono_aot_WebDemo_get_method = Module["_mono_aot_WebDemo_get_method"] = function() {
  return (_mono_aot_WebDemo_get_method = Module["_mono_aot_WebDemo_get_method"] = Module["asm"]["mono_aot_WebDemo_get_method"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var _free = Module["_free"] = function() {
  return (_free = Module["_free"] = Module["asm"]["free"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var _malloc = Module["_malloc"] = function() {
  return (_malloc = Module["_malloc"] = Module["asm"]["malloc"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var ___errno_location = Module["___errno_location"] = function() {
  return (___errno_location = Module["___errno_location"] = Module["asm"]["__errno_location"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var _mono_wasm_register_root = Module["_mono_wasm_register_root"] = function() {
  return (_mono_wasm_register_root = Module["_mono_wasm_register_root"] = Module["asm"]["mono_wasm_register_root"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var _mono_wasm_deregister_root = Module["_mono_wasm_deregister_root"] = function() {
  return (_mono_wasm_deregister_root = Module["_mono_wasm_deregister_root"] = Module["asm"]["mono_wasm_deregister_root"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var _mono_wasm_add_assembly = Module["_mono_wasm_add_assembly"] = function() {
  return (_mono_wasm_add_assembly = Module["_mono_wasm_add_assembly"] = Module["asm"]["mono_wasm_add_assembly"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var _mono_wasm_add_satellite_assembly = Module["_mono_wasm_add_satellite_assembly"] = function() {
  return (_mono_wasm_add_satellite_assembly = Module["_mono_wasm_add_satellite_assembly"] = Module["asm"]["mono_wasm_add_satellite_assembly"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var _mono_wasm_setenv = Module["_mono_wasm_setenv"] = function() {
  return (_mono_wasm_setenv = Module["_mono_wasm_setenv"] = Module["asm"]["mono_wasm_setenv"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var _mono_wasm_getenv = Module["_mono_wasm_getenv"] = function() {
  return (_mono_wasm_getenv = Module["_mono_wasm_getenv"] = Module["asm"]["mono_wasm_getenv"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var _mono_wasm_register_bundled_satellite_assemblies = Module["_mono_wasm_register_bundled_satellite_assemblies"] = function() {
  return (_mono_wasm_register_bundled_satellite_assemblies = Module["_mono_wasm_register_bundled_satellite_assemblies"] = Module["asm"]["mono_wasm_register_bundled_satellite_assemblies"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var _mono_wasm_load_runtime = Module["_mono_wasm_load_runtime"] = function() {
  return (_mono_wasm_load_runtime = Module["_mono_wasm_load_runtime"] = Module["asm"]["mono_wasm_load_runtime"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var _mono_wasm_assembly_load = Module["_mono_wasm_assembly_load"] = function() {
  return (_mono_wasm_assembly_load = Module["_mono_wasm_assembly_load"] = Module["asm"]["mono_wasm_assembly_load"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var _mono_wasm_get_corlib = Module["_mono_wasm_get_corlib"] = function() {
  return (_mono_wasm_get_corlib = Module["_mono_wasm_get_corlib"] = Module["asm"]["mono_wasm_get_corlib"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var _mono_wasm_assembly_find_class = Module["_mono_wasm_assembly_find_class"] = function() {
  return (_mono_wasm_assembly_find_class = Module["_mono_wasm_assembly_find_class"] = Module["asm"]["mono_wasm_assembly_find_class"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var _mono_wasm_runtime_run_module_cctor = Module["_mono_wasm_runtime_run_module_cctor"] = function() {
  return (_mono_wasm_runtime_run_module_cctor = Module["_mono_wasm_runtime_run_module_cctor"] = Module["asm"]["mono_wasm_runtime_run_module_cctor"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var _mono_wasm_assembly_find_method = Module["_mono_wasm_assembly_find_method"] = function() {
  return (_mono_wasm_assembly_find_method = Module["_mono_wasm_assembly_find_method"] = Module["asm"]["mono_wasm_assembly_find_method"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var _mono_wasm_get_delegate_invoke_ref = Module["_mono_wasm_get_delegate_invoke_ref"] = function() {
  return (_mono_wasm_get_delegate_invoke_ref = Module["_mono_wasm_get_delegate_invoke_ref"] = Module["asm"]["mono_wasm_get_delegate_invoke_ref"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var _mono_wasm_box_primitive_ref = Module["_mono_wasm_box_primitive_ref"] = function() {
  return (_mono_wasm_box_primitive_ref = Module["_mono_wasm_box_primitive_ref"] = Module["asm"]["mono_wasm_box_primitive_ref"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var _mono_wasm_invoke_method_ref = Module["_mono_wasm_invoke_method_ref"] = function() {
  return (_mono_wasm_invoke_method_ref = Module["_mono_wasm_invoke_method_ref"] = Module["asm"]["mono_wasm_invoke_method_ref"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var _mono_wasm_invoke_method_bound = Module["_mono_wasm_invoke_method_bound"] = function() {
  return (_mono_wasm_invoke_method_bound = Module["_mono_wasm_invoke_method_bound"] = Module["asm"]["mono_wasm_invoke_method_bound"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var _mono_wasm_assembly_get_entry_point = Module["_mono_wasm_assembly_get_entry_point"] = function() {
  return (_mono_wasm_assembly_get_entry_point = Module["_mono_wasm_assembly_get_entry_point"] = Module["asm"]["mono_wasm_assembly_get_entry_point"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var _mono_wasm_string_get_utf8 = Module["_mono_wasm_string_get_utf8"] = function() {
  return (_mono_wasm_string_get_utf8 = Module["_mono_wasm_string_get_utf8"] = Module["asm"]["mono_wasm_string_get_utf8"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var _mono_wasm_string_from_js = Module["_mono_wasm_string_from_js"] = function() {
  return (_mono_wasm_string_from_js = Module["_mono_wasm_string_from_js"] = Module["asm"]["mono_wasm_string_from_js"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var _mono_wasm_string_from_utf16_ref = Module["_mono_wasm_string_from_utf16_ref"] = function() {
  return (_mono_wasm_string_from_utf16_ref = Module["_mono_wasm_string_from_utf16_ref"] = Module["asm"]["mono_wasm_string_from_utf16_ref"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var _mono_wasm_get_obj_class = Module["_mono_wasm_get_obj_class"] = function() {
  return (_mono_wasm_get_obj_class = Module["_mono_wasm_get_obj_class"] = Module["asm"]["mono_wasm_get_obj_class"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var _mono_wasm_get_obj_type = Module["_mono_wasm_get_obj_type"] = function() {
  return (_mono_wasm_get_obj_type = Module["_mono_wasm_get_obj_type"] = Module["asm"]["mono_wasm_get_obj_type"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var _mono_wasm_try_unbox_primitive_and_get_type_ref = Module["_mono_wasm_try_unbox_primitive_and_get_type_ref"] = function() {
  return (_mono_wasm_try_unbox_primitive_and_get_type_ref = Module["_mono_wasm_try_unbox_primitive_and_get_type_ref"] = Module["asm"]["mono_wasm_try_unbox_primitive_and_get_type_ref"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var _mono_wasm_array_length = Module["_mono_wasm_array_length"] = function() {
  return (_mono_wasm_array_length = Module["_mono_wasm_array_length"] = Module["asm"]["mono_wasm_array_length"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var _mono_wasm_array_get = Module["_mono_wasm_array_get"] = function() {
  return (_mono_wasm_array_get = Module["_mono_wasm_array_get"] = Module["asm"]["mono_wasm_array_get"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var _mono_wasm_array_get_ref = Module["_mono_wasm_array_get_ref"] = function() {
  return (_mono_wasm_array_get_ref = Module["_mono_wasm_array_get_ref"] = Module["asm"]["mono_wasm_array_get_ref"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var _mono_wasm_obj_array_new_ref = Module["_mono_wasm_obj_array_new_ref"] = function() {
  return (_mono_wasm_obj_array_new_ref = Module["_mono_wasm_obj_array_new_ref"] = Module["asm"]["mono_wasm_obj_array_new_ref"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var _mono_wasm_obj_array_new = Module["_mono_wasm_obj_array_new"] = function() {
  return (_mono_wasm_obj_array_new = Module["_mono_wasm_obj_array_new"] = Module["asm"]["mono_wasm_obj_array_new"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var _mono_wasm_obj_array_set = Module["_mono_wasm_obj_array_set"] = function() {
  return (_mono_wasm_obj_array_set = Module["_mono_wasm_obj_array_set"] = Module["asm"]["mono_wasm_obj_array_set"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var _mono_wasm_obj_array_set_ref = Module["_mono_wasm_obj_array_set_ref"] = function() {
  return (_mono_wasm_obj_array_set_ref = Module["_mono_wasm_obj_array_set_ref"] = Module["asm"]["mono_wasm_obj_array_set_ref"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var _mono_wasm_string_array_new_ref = Module["_mono_wasm_string_array_new_ref"] = function() {
  return (_mono_wasm_string_array_new_ref = Module["_mono_wasm_string_array_new_ref"] = Module["asm"]["mono_wasm_string_array_new_ref"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var _mono_wasm_exec_regression = Module["_mono_wasm_exec_regression"] = function() {
  return (_mono_wasm_exec_regression = Module["_mono_wasm_exec_regression"] = Module["asm"]["mono_wasm_exec_regression"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var _mono_wasm_exit = Module["_mono_wasm_exit"] = function() {
  return (_mono_wasm_exit = Module["_mono_wasm_exit"] = Module["asm"]["mono_wasm_exit"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var _mono_wasm_set_main_args = Module["_mono_wasm_set_main_args"] = function() {
  return (_mono_wasm_set_main_args = Module["_mono_wasm_set_main_args"] = Module["asm"]["mono_wasm_set_main_args"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var _mono_wasm_strdup = Module["_mono_wasm_strdup"] = function() {
  return (_mono_wasm_strdup = Module["_mono_wasm_strdup"] = Module["asm"]["mono_wasm_strdup"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var _mono_wasm_parse_runtime_options = Module["_mono_wasm_parse_runtime_options"] = function() {
  return (_mono_wasm_parse_runtime_options = Module["_mono_wasm_parse_runtime_options"] = Module["asm"]["mono_wasm_parse_runtime_options"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var _mono_wasm_enable_on_demand_gc = Module["_mono_wasm_enable_on_demand_gc"] = function() {
  return (_mono_wasm_enable_on_demand_gc = Module["_mono_wasm_enable_on_demand_gc"] = Module["asm"]["mono_wasm_enable_on_demand_gc"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var _mono_wasm_intern_string_ref = Module["_mono_wasm_intern_string_ref"] = function() {
  return (_mono_wasm_intern_string_ref = Module["_mono_wasm_intern_string_ref"] = Module["asm"]["mono_wasm_intern_string_ref"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var _mono_wasm_string_get_data_ref = Module["_mono_wasm_string_get_data_ref"] = function() {
  return (_mono_wasm_string_get_data_ref = Module["_mono_wasm_string_get_data_ref"] = Module["asm"]["mono_wasm_string_get_data_ref"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var _mono_wasm_string_get_data = Module["_mono_wasm_string_get_data"] = function() {
  return (_mono_wasm_string_get_data = Module["_mono_wasm_string_get_data"] = Module["asm"]["mono_wasm_string_get_data"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var _mono_wasm_class_get_type = Module["_mono_wasm_class_get_type"] = function() {
  return (_mono_wasm_class_get_type = Module["_mono_wasm_class_get_type"] = Module["asm"]["mono_wasm_class_get_type"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var _mono_wasm_type_get_class = Module["_mono_wasm_type_get_class"] = function() {
  return (_mono_wasm_type_get_class = Module["_mono_wasm_type_get_class"] = Module["asm"]["mono_wasm_type_get_class"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var _mono_wasm_get_type_name = Module["_mono_wasm_get_type_name"] = function() {
  return (_mono_wasm_get_type_name = Module["_mono_wasm_get_type_name"] = Module["asm"]["mono_wasm_get_type_name"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var _mono_wasm_get_type_aqn = Module["_mono_wasm_get_type_aqn"] = function() {
  return (_mono_wasm_get_type_aqn = Module["_mono_wasm_get_type_aqn"] = Module["asm"]["mono_wasm_get_type_aqn"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var _mono_wasm_write_managed_pointer_unsafe = Module["_mono_wasm_write_managed_pointer_unsafe"] = function() {
  return (_mono_wasm_write_managed_pointer_unsafe = Module["_mono_wasm_write_managed_pointer_unsafe"] = Module["asm"]["mono_wasm_write_managed_pointer_unsafe"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var _mono_wasm_copy_managed_pointer = Module["_mono_wasm_copy_managed_pointer"] = function() {
  return (_mono_wasm_copy_managed_pointer = Module["_mono_wasm_copy_managed_pointer"] = Module["asm"]["mono_wasm_copy_managed_pointer"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var _mono_wasm_i52_to_f64 = Module["_mono_wasm_i52_to_f64"] = function() {
  return (_mono_wasm_i52_to_f64 = Module["_mono_wasm_i52_to_f64"] = Module["asm"]["mono_wasm_i52_to_f64"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var _mono_wasm_u52_to_f64 = Module["_mono_wasm_u52_to_f64"] = function() {
  return (_mono_wasm_u52_to_f64 = Module["_mono_wasm_u52_to_f64"] = Module["asm"]["mono_wasm_u52_to_f64"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var _mono_wasm_f64_to_u52 = Module["_mono_wasm_f64_to_u52"] = function() {
  return (_mono_wasm_f64_to_u52 = Module["_mono_wasm_f64_to_u52"] = Module["asm"]["mono_wasm_f64_to_u52"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var _mono_wasm_f64_to_i52 = Module["_mono_wasm_f64_to_i52"] = function() {
  return (_mono_wasm_f64_to_i52 = Module["_mono_wasm_f64_to_i52"] = Module["asm"]["mono_wasm_f64_to_i52"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var _mono_wasm_typed_array_new_ref = Module["_mono_wasm_typed_array_new_ref"] = function() {
  return (_mono_wasm_typed_array_new_ref = Module["_mono_wasm_typed_array_new_ref"] = Module["asm"]["mono_wasm_typed_array_new_ref"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var _mono_wasm_unbox_enum = Module["_mono_wasm_unbox_enum"] = function() {
  return (_mono_wasm_unbox_enum = Module["_mono_wasm_unbox_enum"] = Module["asm"]["mono_wasm_unbox_enum"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var _mono_wasm_set_is_debugger_attached = Module["_mono_wasm_set_is_debugger_attached"] = function() {
  return (_mono_wasm_set_is_debugger_attached = Module["_mono_wasm_set_is_debugger_attached"] = Module["asm"]["mono_wasm_set_is_debugger_attached"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var _mono_wasm_change_debugger_log_level = Module["_mono_wasm_change_debugger_log_level"] = function() {
  return (_mono_wasm_change_debugger_log_level = Module["_mono_wasm_change_debugger_log_level"] = Module["asm"]["mono_wasm_change_debugger_log_level"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var _mono_wasm_send_dbg_command_with_parms = Module["_mono_wasm_send_dbg_command_with_parms"] = function() {
  return (_mono_wasm_send_dbg_command_with_parms = Module["_mono_wasm_send_dbg_command_with_parms"] = Module["asm"]["mono_wasm_send_dbg_command_with_parms"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var _mono_wasm_send_dbg_command = Module["_mono_wasm_send_dbg_command"] = function() {
  return (_mono_wasm_send_dbg_command = Module["_mono_wasm_send_dbg_command"] = Module["asm"]["mono_wasm_send_dbg_command"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var _mono_wasm_event_pipe_enable = Module["_mono_wasm_event_pipe_enable"] = function() {
  return (_mono_wasm_event_pipe_enable = Module["_mono_wasm_event_pipe_enable"] = Module["asm"]["mono_wasm_event_pipe_enable"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var _mono_wasm_event_pipe_session_start_streaming = Module["_mono_wasm_event_pipe_session_start_streaming"] = function() {
  return (_mono_wasm_event_pipe_session_start_streaming = Module["_mono_wasm_event_pipe_session_start_streaming"] = Module["asm"]["mono_wasm_event_pipe_session_start_streaming"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var _mono_wasm_event_pipe_session_disable = Module["_mono_wasm_event_pipe_session_disable"] = function() {
  return (_mono_wasm_event_pipe_session_disable = Module["_mono_wasm_event_pipe_session_disable"] = Module["asm"]["mono_wasm_event_pipe_session_disable"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var _emscripten_stack_get_end = Module["_emscripten_stack_get_end"] = function() {
  return (_emscripten_stack_get_end = Module["_emscripten_stack_get_end"] = Module["asm"]["emscripten_stack_get_end"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var _emscripten_stack_get_base = Module["_emscripten_stack_get_base"] = function() {
  return (_emscripten_stack_get_base = Module["_emscripten_stack_get_base"] = Module["asm"]["emscripten_stack_get_base"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var _mono_background_exec = Module["_mono_background_exec"] = function() {
  return (_mono_background_exec = Module["_mono_background_exec"] = Module["asm"]["mono_background_exec"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var _mono_print_method_from_ip = Module["_mono_print_method_from_ip"] = function() {
  return (_mono_print_method_from_ip = Module["_mono_print_method_from_ip"] = Module["asm"]["mono_print_method_from_ip"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var _mono_set_timeout_exec = Module["_mono_set_timeout_exec"] = function() {
  return (_mono_set_timeout_exec = Module["_mono_set_timeout_exec"] = Module["asm"]["mono_set_timeout_exec"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var _emscripten_builtin_malloc = Module["_emscripten_builtin_malloc"] = function() {
  return (_emscripten_builtin_malloc = Module["_emscripten_builtin_malloc"] = Module["asm"]["emscripten_builtin_malloc"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var ___dl_seterr = Module["___dl_seterr"] = function() {
  return (___dl_seterr = Module["___dl_seterr"] = Module["asm"]["__dl_seterr"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var _htonl = Module["_htonl"] = function() {
  return (_htonl = Module["_htonl"] = Module["asm"]["htonl"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var _htons = Module["_htons"] = function() {
  return (_htons = Module["_htons"] = Module["asm"]["htons"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var _emscripten_builtin_free = Module["_emscripten_builtin_free"] = function() {
  return (_emscripten_builtin_free = Module["_emscripten_builtin_free"] = Module["asm"]["emscripten_builtin_free"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var _emscripten_builtin_memalign = Module["_emscripten_builtin_memalign"] = function() {
  return (_emscripten_builtin_memalign = Module["_emscripten_builtin_memalign"] = Module["asm"]["emscripten_builtin_memalign"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var _ntohs = Module["_ntohs"] = function() {
  return (_ntohs = Module["_ntohs"] = Module["asm"]["ntohs"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var _memalign = Module["_memalign"] = function() {
  return (_memalign = Module["_memalign"] = Module["asm"]["memalign"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var _setThrew = Module["_setThrew"] = function() {
  return (_setThrew = Module["_setThrew"] = Module["asm"]["setThrew"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var _emscripten_stack_set_limits = Module["_emscripten_stack_set_limits"] = function() {
  return (_emscripten_stack_set_limits = Module["_emscripten_stack_set_limits"] = Module["asm"]["emscripten_stack_set_limits"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var stackSave = Module["stackSave"] = function() {
  return (stackSave = Module["stackSave"] = Module["asm"]["stackSave"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var stackRestore = Module["stackRestore"] = function() {
  return (stackRestore = Module["stackRestore"] = Module["asm"]["stackRestore"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var stackAlloc = Module["stackAlloc"] = function() {
  return (stackAlloc = Module["stackAlloc"] = Module["asm"]["stackAlloc"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var ___cxa_can_catch = Module["___cxa_can_catch"] = function() {
  return (___cxa_can_catch = Module["___cxa_can_catch"] = Module["asm"]["__cxa_can_catch"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var ___cxa_is_pointer_type = Module["___cxa_is_pointer_type"] = function() {
  return (___cxa_is_pointer_type = Module["___cxa_is_pointer_type"] = Module["asm"]["__cxa_is_pointer_type"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_v = Module["dynCall_v"] = function() {
  return (dynCall_v = Module["dynCall_v"] = Module["asm"]["dynCall_v"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_ii = Module["dynCall_ii"] = function() {
  return (dynCall_ii = Module["dynCall_ii"] = Module["asm"]["dynCall_ii"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_vii = Module["dynCall_vii"] = function() {
  return (dynCall_vii = Module["dynCall_vii"] = Module["asm"]["dynCall_vii"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_viii = Module["dynCall_viii"] = function() {
  return (dynCall_viii = Module["dynCall_viii"] = Module["asm"]["dynCall_viii"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_iii = Module["dynCall_iii"] = function() {
  return (dynCall_iii = Module["dynCall_iii"] = Module["asm"]["dynCall_iii"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_viiiii = Module["dynCall_viiiii"] = function() {
  return (dynCall_viiiii = Module["dynCall_viiiii"] = Module["asm"]["dynCall_viiiii"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_iiii = Module["dynCall_iiii"] = function() {
  return (dynCall_iiii = Module["dynCall_iiii"] = Module["asm"]["dynCall_iiii"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_fii = Module["dynCall_fii"] = function() {
  return (dynCall_fii = Module["dynCall_fii"] = Module["asm"]["dynCall_fii"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_viiii = Module["dynCall_viiii"] = function() {
  return (dynCall_viiii = Module["dynCall_viiii"] = Module["asm"]["dynCall_viiii"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_vijjiiii = Module["dynCall_vijjiiii"] = function() {
  return (dynCall_vijjiiii = Module["dynCall_vijjiiii"] = Module["asm"]["dynCall_vijjiiii"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_vifii = Module["dynCall_vifii"] = function() {
  return (dynCall_vifii = Module["dynCall_vifii"] = Module["asm"]["dynCall_vifii"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_viiiifi = Module["dynCall_viiiifi"] = function() {
  return (dynCall_viiiifi = Module["dynCall_viiiifi"] = Module["asm"]["dynCall_viiiifi"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_viiiiii = Module["dynCall_viiiiii"] = function() {
  return (dynCall_viiiiii = Module["dynCall_viiiiii"] = Module["asm"]["dynCall_viiiiii"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_vi = Module["dynCall_vi"] = function() {
  return (dynCall_vi = Module["dynCall_vi"] = Module["asm"]["dynCall_vi"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_iiiiii = Module["dynCall_iiiiii"] = function() {
  return (dynCall_iiiiii = Module["dynCall_iiiiii"] = Module["asm"]["dynCall_iiiiii"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_vifffffffffi = Module["dynCall_vifffffffffi"] = function() {
  return (dynCall_vifffffffffi = Module["dynCall_vifffffffffi"] = Module["asm"]["dynCall_vifffffffffi"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_viifi = Module["dynCall_viifi"] = function() {
  return (dynCall_viifi = Module["dynCall_viifi"] = Module["asm"]["dynCall_viifi"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_viffffi = Module["dynCall_viffffi"] = function() {
  return (dynCall_viffffi = Module["dynCall_viffffi"] = Module["asm"]["dynCall_viffffi"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_vifffi = Module["dynCall_vifffi"] = function() {
  return (dynCall_vifffi = Module["dynCall_vifffi"] = Module["asm"]["dynCall_vifffi"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_vifi = Module["dynCall_vifi"] = function() {
  return (dynCall_vifi = Module["dynCall_vifi"] = Module["asm"]["dynCall_vifi"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_fiii = Module["dynCall_fiii"] = function() {
  return (dynCall_fiii = Module["dynCall_fiii"] = Module["asm"]["dynCall_fiii"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_iiiii = Module["dynCall_iiiii"] = function() {
  return (dynCall_iiiii = Module["dynCall_iiiii"] = Module["asm"]["dynCall_iiiii"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_vijji = Module["dynCall_vijji"] = function() {
  return (dynCall_vijji = Module["dynCall_vijji"] = Module["asm"]["dynCall_vijji"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_fiiiii = Module["dynCall_fiiiii"] = function() {
  return (dynCall_fiiiii = Module["dynCall_fiiiii"] = Module["asm"]["dynCall_fiiiii"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_viiiiiififfi = Module["dynCall_viiiiiififfi"] = function() {
  return (dynCall_viiiiiififfi = Module["dynCall_viiiiiififfi"] = Module["asm"]["dynCall_viiiiiififfi"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_viiifii = Module["dynCall_viiifii"] = function() {
  return (dynCall_viiifii = Module["dynCall_viiifii"] = Module["asm"]["dynCall_viiifii"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_iiiiiiiii = Module["dynCall_iiiiiiiii"] = function() {
  return (dynCall_iiiiiiiii = Module["dynCall_iiiiiiiii"] = Module["asm"]["dynCall_iiiiiiiii"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_fji = Module["dynCall_fji"] = function() {
  return (dynCall_fji = Module["dynCall_fji"] = Module["asm"]["dynCall_fji"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_diii = Module["dynCall_diii"] = function() {
  return (dynCall_diii = Module["dynCall_diii"] = Module["asm"]["dynCall_diii"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_iiiiiiiiiiii = Module["dynCall_iiiiiiiiiiii"] = function() {
  return (dynCall_iiiiiiiiiiii = Module["dynCall_iiiiiiiiiiii"] = Module["asm"]["dynCall_iiiiiiiiiiii"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_iiiiiii = Module["dynCall_iiiiiii"] = function() {
  return (dynCall_iiiiiii = Module["dynCall_iiiiiii"] = Module["asm"]["dynCall_iiiiiii"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_iiiji = Module["dynCall_iiiji"] = function() {
  return (dynCall_iiiji = Module["dynCall_iiiji"] = Module["asm"]["dynCall_iiiji"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_viffi = Module["dynCall_viffi"] = function() {
  return (dynCall_viffi = Module["dynCall_viffi"] = Module["asm"]["dynCall_viffi"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_viiiiiii = Module["dynCall_viiiiiii"] = function() {
  return (dynCall_viiiiiii = Module["dynCall_viiiiiii"] = Module["asm"]["dynCall_viiiiiii"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_ji = Module["dynCall_ji"] = function() {
  return (dynCall_ji = Module["dynCall_ji"] = Module["asm"]["dynCall_ji"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_viiiiiiiiiii = Module["dynCall_viiiiiiiiiii"] = function() {
  return (dynCall_viiiiiiiiiii = Module["dynCall_viiiiiiiiiii"] = Module["asm"]["dynCall_viiiiiiiiiii"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_vffi = Module["dynCall_vffi"] = function() {
  return (dynCall_vffi = Module["dynCall_vffi"] = Module["asm"]["dynCall_vffi"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_viiiiiiiii = Module["dynCall_viiiiiiiii"] = function() {
  return (dynCall_viiiiiiiii = Module["dynCall_viiiiiiiii"] = Module["asm"]["dynCall_viiiiiiiii"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_viiiiiiiiiiii = Module["dynCall_viiiiiiiiiiii"] = function() {
  return (dynCall_viiiiiiiiiiii = Module["dynCall_viiiiiiiiiiii"] = Module["asm"]["dynCall_viiiiiiiiiiii"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_ifi = Module["dynCall_ifi"] = function() {
  return (dynCall_ifi = Module["dynCall_ifi"] = Module["asm"]["dynCall_ifi"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_iiffi = Module["dynCall_iiffi"] = function() {
  return (dynCall_iiffi = Module["dynCall_iiffi"] = Module["asm"]["dynCall_iiffi"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_vfi = Module["dynCall_vfi"] = function() {
  return (dynCall_vfi = Module["dynCall_vfi"] = Module["asm"]["dynCall_vfi"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_viffiii = Module["dynCall_viffiii"] = function() {
  return (dynCall_viffiii = Module["dynCall_viffiii"] = Module["asm"]["dynCall_viffiii"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_vifiii = Module["dynCall_vifiii"] = function() {
  return (dynCall_vifiii = Module["dynCall_vifiii"] = Module["asm"]["dynCall_vifiii"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_viifii = Module["dynCall_viifii"] = function() {
  return (dynCall_viifii = Module["dynCall_viifii"] = Module["asm"]["dynCall_viifii"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_iiiiiiijiii = Module["dynCall_iiiiiiijiii"] = function() {
  return (dynCall_iiiiiiijiii = Module["dynCall_iiiiiiijiii"] = Module["asm"]["dynCall_iiiiiiijiii"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_iiiiiiii = Module["dynCall_iiiiiiii"] = function() {
  return (dynCall_iiiiiiii = Module["dynCall_iiiiiiii"] = Module["asm"]["dynCall_iiiiiiii"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_iiiiiiji = Module["dynCall_iiiiiiji"] = function() {
  return (dynCall_iiiiiiji = Module["dynCall_iiiiiiji"] = Module["asm"]["dynCall_iiiiiiji"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_jii = Module["dynCall_jii"] = function() {
  return (dynCall_jii = Module["dynCall_jii"] = Module["asm"]["dynCall_jii"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_viiiiiiii = Module["dynCall_viiiiiiii"] = function() {
  return (dynCall_viiiiiiii = Module["dynCall_viiiiiiii"] = Module["asm"]["dynCall_viiiiiiii"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_iiiiiji = Module["dynCall_iiiiiji"] = function() {
  return (dynCall_iiiiiji = Module["dynCall_iiiiiji"] = Module["asm"]["dynCall_iiiiiji"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_i = Module["dynCall_i"] = function() {
  return (dynCall_i = Module["dynCall_i"] = Module["asm"]["dynCall_i"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_iiji = Module["dynCall_iiji"] = function() {
  return (dynCall_iiji = Module["dynCall_iiji"] = Module["asm"]["dynCall_iiji"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_jijii = Module["dynCall_jijii"] = function() {
  return (dynCall_jijii = Module["dynCall_jijii"] = Module["asm"]["dynCall_jijii"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_iijjii = Module["dynCall_iijjii"] = function() {
  return (dynCall_iijjii = Module["dynCall_iijjii"] = Module["asm"]["dynCall_iijjii"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_iijji = Module["dynCall_iijji"] = function() {
  return (dynCall_iijji = Module["dynCall_iijji"] = Module["asm"]["dynCall_iijji"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_iiiiji = Module["dynCall_iiiiji"] = function() {
  return (dynCall_iiiiji = Module["dynCall_iiiiji"] = Module["asm"]["dynCall_iiiiji"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_iiiiiijiii = Module["dynCall_iiiiiijiii"] = function() {
  return (dynCall_iiiiiijiii = Module["dynCall_iiiiiijiii"] = Module["asm"]["dynCall_iiiiiijiii"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_iiiiiijiiiii = Module["dynCall_iiiiiijiiiii"] = function() {
  return (dynCall_iiiiiijiiiii = Module["dynCall_iiiiiijiiiii"] = Module["asm"]["dynCall_iiiiiijiiiii"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_viiji = Module["dynCall_viiji"] = function() {
  return (dynCall_viiji = Module["dynCall_viiji"] = Module["asm"]["dynCall_viiji"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_iiiiiiiiii = Module["dynCall_iiiiiiiiii"] = function() {
  return (dynCall_iiiiiiiiii = Module["dynCall_iiiiiiiiii"] = Module["asm"]["dynCall_iiiiiiiiii"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_jiii = Module["dynCall_jiii"] = function() {
  return (dynCall_jiii = Module["dynCall_jiii"] = Module["asm"]["dynCall_jiii"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_ddi = Module["dynCall_ddi"] = function() {
  return (dynCall_ddi = Module["dynCall_ddi"] = Module["asm"]["dynCall_ddi"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_dddi = Module["dynCall_dddi"] = function() {
  return (dynCall_dddi = Module["dynCall_dddi"] = Module["asm"]["dynCall_dddi"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_ddii = Module["dynCall_ddii"] = function() {
  return (dynCall_ddii = Module["dynCall_ddii"] = Module["asm"]["dynCall_ddii"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_ffi = Module["dynCall_ffi"] = function() {
  return (dynCall_ffi = Module["dynCall_ffi"] = Module["asm"]["dynCall_ffi"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_jjjii = Module["dynCall_jjjii"] = function() {
  return (dynCall_jjjii = Module["dynCall_jjjii"] = Module["asm"]["dynCall_jjjii"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_ffffi = Module["dynCall_ffffi"] = function() {
  return (dynCall_ffffi = Module["dynCall_ffffi"] = Module["asm"]["dynCall_ffffi"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_fffi = Module["dynCall_fffi"] = function() {
  return (dynCall_fffi = Module["dynCall_fffi"] = Module["asm"]["dynCall_fffi"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_jjji = Module["dynCall_jjji"] = function() {
  return (dynCall_jjji = Module["dynCall_jjji"] = Module["asm"]["dynCall_jjji"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_ddiii = Module["dynCall_ddiii"] = function() {
  return (dynCall_ddiii = Module["dynCall_ddiii"] = Module["asm"]["dynCall_ddiii"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_idi = Module["dynCall_idi"] = function() {
  return (dynCall_idi = Module["dynCall_idi"] = Module["asm"]["dynCall_idi"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_jdi = Module["dynCall_jdi"] = function() {
  return (dynCall_jdi = Module["dynCall_jdi"] = Module["asm"]["dynCall_jdi"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_dji = Module["dynCall_dji"] = function() {
  return (dynCall_dji = Module["dynCall_dji"] = Module["asm"]["dynCall_dji"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_iji = Module["dynCall_iji"] = function() {
  return (dynCall_iji = Module["dynCall_iji"] = Module["asm"]["dynCall_iji"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_jji = Module["dynCall_jji"] = function() {
  return (dynCall_jji = Module["dynCall_jji"] = Module["asm"]["dynCall_jji"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_jfi = Module["dynCall_jfi"] = function() {
  return (dynCall_jfi = Module["dynCall_jfi"] = Module["asm"]["dynCall_jfi"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_fdi = Module["dynCall_fdi"] = function() {
  return (dynCall_fdi = Module["dynCall_fdi"] = Module["asm"]["dynCall_fdi"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_dii = Module["dynCall_dii"] = function() {
  return (dynCall_dii = Module["dynCall_dii"] = Module["asm"]["dynCall_dii"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_dfi = Module["dynCall_dfi"] = function() {
  return (dynCall_dfi = Module["dynCall_dfi"] = Module["asm"]["dynCall_dfi"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_viji = Module["dynCall_viji"] = function() {
  return (dynCall_viji = Module["dynCall_viji"] = Module["asm"]["dynCall_viji"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_vidi = Module["dynCall_vidi"] = function() {
  return (dynCall_vidi = Module["dynCall_vidi"] = Module["asm"]["dynCall_vidi"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_vijii = Module["dynCall_vijii"] = function() {
  return (dynCall_vijii = Module["dynCall_vijii"] = Module["asm"]["dynCall_vijii"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_vijiii = Module["dynCall_vijiii"] = function() {
  return (dynCall_vijiii = Module["dynCall_vijiii"] = Module["asm"]["dynCall_vijiii"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_jiiiiiiiii = Module["dynCall_jiiiiiiiii"] = function() {
  return (dynCall_jiiiiiiiii = Module["dynCall_jiiiiiiiii"] = Module["asm"]["dynCall_jiiiiiiiii"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_viidjji = Module["dynCall_viidjji"] = function() {
  return (dynCall_viidjji = Module["dynCall_viidjji"] = Module["asm"]["dynCall_viidjji"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_viidi = Module["dynCall_viidi"] = function() {
  return (dynCall_viidi = Module["dynCall_viidi"] = Module["asm"]["dynCall_viidi"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_iijii = Module["dynCall_iijii"] = function() {
  return (dynCall_iijii = Module["dynCall_iijii"] = Module["asm"]["dynCall_iijii"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_jiiii = Module["dynCall_jiiii"] = function() {
  return (dynCall_jiiii = Module["dynCall_jiiii"] = Module["asm"]["dynCall_jiiii"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_vjjii = Module["dynCall_vjjii"] = function() {
  return (dynCall_vjjii = Module["dynCall_vjjii"] = Module["asm"]["dynCall_vjjii"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_vfii = Module["dynCall_vfii"] = function() {
  return (dynCall_vfii = Module["dynCall_vfii"] = Module["asm"]["dynCall_vfii"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_vdii = Module["dynCall_vdii"] = function() {
  return (dynCall_vdii = Module["dynCall_vdii"] = Module["asm"]["dynCall_vdii"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_iidi = Module["dynCall_iidi"] = function() {
  return (dynCall_iidi = Module["dynCall_iidi"] = Module["asm"]["dynCall_iidi"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_iddi = Module["dynCall_iddi"] = function() {
  return (dynCall_iddi = Module["dynCall_iddi"] = Module["asm"]["dynCall_iddi"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_vjii = Module["dynCall_vjii"] = function() {
  return (dynCall_vjii = Module["dynCall_vjii"] = Module["asm"]["dynCall_vjii"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_viiiiiiiiiiiii = Module["dynCall_viiiiiiiiiiiii"] = function() {
  return (dynCall_viiiiiiiiiiiii = Module["dynCall_viiiiiiiiiiiii"] = Module["asm"]["dynCall_viiiiiiiiiiiii"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_ijji = Module["dynCall_ijji"] = function() {
  return (dynCall_ijji = Module["dynCall_ijji"] = Module["asm"]["dynCall_ijji"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_vdiiii = Module["dynCall_vdiiii"] = function() {
  return (dynCall_vdiiii = Module["dynCall_vdiiii"] = Module["asm"]["dynCall_vdiiii"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_vfiiii = Module["dynCall_vfiiii"] = function() {
  return (dynCall_vfiiii = Module["dynCall_vfiiii"] = Module["asm"]["dynCall_vfiiii"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_ijiiiiiiii = Module["dynCall_ijiiiiiiii"] = function() {
  return (dynCall_ijiiiiiiii = Module["dynCall_ijiiiiiiii"] = Module["asm"]["dynCall_ijiiiiiiii"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_idiii = Module["dynCall_idiii"] = function() {
  return (dynCall_idiii = Module["dynCall_idiii"] = Module["asm"]["dynCall_idiii"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_idiiiii = Module["dynCall_idiiiii"] = function() {
  return (dynCall_idiiiii = Module["dynCall_idiiiii"] = Module["asm"]["dynCall_idiiiii"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_iidiii = Module["dynCall_iidiii"] = function() {
  return (dynCall_iidiii = Module["dynCall_iidiii"] = Module["asm"]["dynCall_iidiii"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_ifiii = Module["dynCall_ifiii"] = function() {
  return (dynCall_ifiii = Module["dynCall_ifiii"] = Module["asm"]["dynCall_ifiii"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_ifiiiii = Module["dynCall_ifiiiii"] = function() {
  return (dynCall_ifiiiii = Module["dynCall_ifiiiii"] = Module["asm"]["dynCall_ifiiiii"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_iifiii = Module["dynCall_iifiii"] = function() {
  return (dynCall_iifiii = Module["dynCall_iifiii"] = Module["asm"]["dynCall_iifiii"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_ijiii = Module["dynCall_ijiii"] = function() {
  return (dynCall_ijiii = Module["dynCall_ijiii"] = Module["asm"]["dynCall_ijiii"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_ijiiiii = Module["dynCall_ijiiiii"] = function() {
  return (dynCall_ijiiiii = Module["dynCall_ijiiiii"] = Module["asm"]["dynCall_ijiiiii"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_iijiii = Module["dynCall_iijiii"] = function() {
  return (dynCall_iijiii = Module["dynCall_iijiii"] = Module["asm"]["dynCall_iijiii"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_ijii = Module["dynCall_ijii"] = function() {
  return (dynCall_ijii = Module["dynCall_ijii"] = Module["asm"]["dynCall_ijii"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_ijiiii = Module["dynCall_ijiiii"] = function() {
  return (dynCall_ijiiii = Module["dynCall_ijiiii"] = Module["asm"]["dynCall_ijiiii"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_jdii = Module["dynCall_jdii"] = function() {
  return (dynCall_jdii = Module["dynCall_jdii"] = Module["asm"]["dynCall_jdii"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_ifii = Module["dynCall_ifii"] = function() {
  return (dynCall_ifii = Module["dynCall_ifii"] = Module["asm"]["dynCall_ifii"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_jijiii = Module["dynCall_jijiii"] = function() {
  return (dynCall_jijiii = Module["dynCall_jijiii"] = Module["asm"]["dynCall_jijiii"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_jiiiii = Module["dynCall_jiiiii"] = function() {
  return (dynCall_jiiiii = Module["dynCall_jiiiii"] = Module["asm"]["dynCall_jiiiii"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_jiiiiii = Module["dynCall_jiiiiii"] = function() {
  return (dynCall_jiiiiii = Module["dynCall_jiiiiii"] = Module["asm"]["dynCall_jiiiiii"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_jjiii = Module["dynCall_jjiii"] = function() {
  return (dynCall_jjiii = Module["dynCall_jjiii"] = Module["asm"]["dynCall_jjiii"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_vijjii = Module["dynCall_vijjii"] = function() {
  return (dynCall_vijjii = Module["dynCall_vijjii"] = Module["asm"]["dynCall_vijjii"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_viijji = Module["dynCall_viijji"] = function() {
  return (dynCall_viijji = Module["dynCall_viijji"] = Module["asm"]["dynCall_viijji"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_diiii = Module["dynCall_diiii"] = function() {
  return (dynCall_diiii = Module["dynCall_diiii"] = Module["asm"]["dynCall_diiii"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_fiiii = Module["dynCall_fiiii"] = function() {
  return (dynCall_fiiii = Module["dynCall_fiiii"] = Module["asm"]["dynCall_fiiii"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_ijjiiii = Module["dynCall_ijjiiii"] = function() {
  return (dynCall_ijjiiii = Module["dynCall_ijjiiii"] = Module["asm"]["dynCall_ijjiiii"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_vidiii = Module["dynCall_vidiii"] = function() {
  return (dynCall_vidiii = Module["dynCall_vidiii"] = Module["asm"]["dynCall_vidiii"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_iiijjjii = Module["dynCall_iiijjjii"] = function() {
  return (dynCall_iiijjjii = Module["dynCall_iiijjjii"] = Module["asm"]["dynCall_iiijjjii"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_iiijjjjji = Module["dynCall_iiijjjjji"] = function() {
  return (dynCall_iiijjjjji = Module["dynCall_iiijjjjji"] = Module["asm"]["dynCall_iiijjjjji"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_viiiiijiiiiiii = Module["dynCall_viiiiijiiiiiii"] = function() {
  return (dynCall_viiiiijiiiiiii = Module["dynCall_viiiiijiiiiiii"] = Module["asm"]["dynCall_viiiiijiiiiiii"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_iifi = Module["dynCall_iifi"] = function() {
  return (dynCall_iifi = Module["dynCall_iifi"] = Module["asm"]["dynCall_iifi"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_iffi = Module["dynCall_iffi"] = function() {
  return (dynCall_iffi = Module["dynCall_iffi"] = Module["asm"]["dynCall_iffi"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_viddi = Module["dynCall_viddi"] = function() {
  return (dynCall_viddi = Module["dynCall_viddi"] = Module["asm"]["dynCall_viddi"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_viiiiiiiiii = Module["dynCall_viiiiiiiiii"] = function() {
  return (dynCall_viiiiiiiiii = Module["dynCall_viiiiiiiiii"] = Module["asm"]["dynCall_viiiiiiiiii"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_iiiiiiiiiii = Module["dynCall_iiiiiiiiiii"] = function() {
  return (dynCall_iiiiiiiiiii = Module["dynCall_iiiiiiiiiii"] = Module["asm"]["dynCall_iiiiiiiiiii"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_viffffffffffffffffi = Module["dynCall_viffffffffffffffffi"] = function() {
  return (dynCall_viffffffffffffffffi = Module["dynCall_viffffffffffffffffi"] = Module["asm"]["dynCall_viffffffffffffffffi"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_jijji = Module["dynCall_jijji"] = function() {
  return (dynCall_jijji = Module["dynCall_jijji"] = Module["asm"]["dynCall_jijji"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_jiji = Module["dynCall_jiji"] = function() {
  return (dynCall_jiji = Module["dynCall_jiji"] = Module["asm"]["dynCall_jiji"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_viiiji = Module["dynCall_viiiji"] = function() {
  return (dynCall_viiiji = Module["dynCall_viiiji"] = Module["asm"]["dynCall_viiiji"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_viijii = Module["dynCall_viijii"] = function() {
  return (dynCall_viijii = Module["dynCall_viijii"] = Module["asm"]["dynCall_viijii"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_viijjii = Module["dynCall_viijjii"] = function() {
  return (dynCall_viijjii = Module["dynCall_viijjii"] = Module["asm"]["dynCall_viijjii"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_viiiiiiji = Module["dynCall_viiiiiiji"] = function() {
  return (dynCall_viiiiiiji = Module["dynCall_viiiiiiji"] = Module["asm"]["dynCall_viiiiiiji"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_viijiiii = Module["dynCall_viijiiii"] = function() {
  return (dynCall_viijiiii = Module["dynCall_viijiiii"] = Module["asm"]["dynCall_viijiiii"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_viiiiji = Module["dynCall_viiiiji"] = function() {
  return (dynCall_viiiiji = Module["dynCall_viiiiji"] = Module["asm"]["dynCall_viiiiji"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_viiiiiiiji = Module["dynCall_viiiiiiiji"] = function() {
  return (dynCall_viiiiiiiji = Module["dynCall_viiiiiiiji"] = Module["asm"]["dynCall_viiiiiiiji"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_viiiijjiii = Module["dynCall_viiiijjiii"] = function() {
  return (dynCall_viiiijjiii = Module["dynCall_viiiijjiii"] = Module["asm"]["dynCall_viiiijjiii"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_iijiiiiiii = Module["dynCall_iijiiiiiii"] = function() {
  return (dynCall_iijiiiiiii = Module["dynCall_iijiiiiiii"] = Module["asm"]["dynCall_iijiiiiiii"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_iiijii = Module["dynCall_iiijii"] = function() {
  return (dynCall_iiijii = Module["dynCall_iiijii"] = Module["asm"]["dynCall_iiijii"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_viijjji = Module["dynCall_viijjji"] = function() {
  return (dynCall_viijjji = Module["dynCall_viijjji"] = Module["asm"]["dynCall_viijjji"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_iiiijijii = Module["dynCall_iiiijijii"] = function() {
  return (dynCall_iiiijijii = Module["dynCall_iiiijijii"] = Module["asm"]["dynCall_iiiijijii"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_viiiiiiiijii = Module["dynCall_viiiiiiiijii"] = function() {
  return (dynCall_viiiiiiiijii = Module["dynCall_viiiiiiiijii"] = Module["asm"]["dynCall_viiiiiiiijii"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_viiiiiiiiiiji = Module["dynCall_viiiiiiiiiiji"] = function() {
  return (dynCall_viiiiiiiiiiji = Module["dynCall_viiiiiiiiiiji"] = Module["asm"]["dynCall_viiiiiiiiiiji"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_jiiji = Module["dynCall_jiiji"] = function() {
  return (dynCall_jiiji = Module["dynCall_jiiji"] = Module["asm"]["dynCall_jiiji"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_viijiiiiii = Module["dynCall_viijiiiiii"] = function() {
  return (dynCall_viijiiiiii = Module["dynCall_viijiiiiii"] = Module["asm"]["dynCall_viijiiiiii"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_viijiii = Module["dynCall_viijiii"] = function() {
  return (dynCall_viijiii = Module["dynCall_viijiii"] = Module["asm"]["dynCall_viijiii"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_vijiiiiii = Module["dynCall_vijiiiiii"] = function() {
  return (dynCall_vijiiiiii = Module["dynCall_vijiiiiii"] = Module["asm"]["dynCall_vijiiiiii"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_vji = Module["dynCall_vji"] = function() {
  return (dynCall_vji = Module["dynCall_vji"] = Module["asm"]["dynCall_vji"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_vdi = Module["dynCall_vdi"] = function() {
  return (dynCall_vdi = Module["dynCall_vdi"] = Module["asm"]["dynCall_vdi"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_viiiiijii = Module["dynCall_viiiiijii"] = function() {
  return (dynCall_viiiiijii = Module["dynCall_viiiiijii"] = Module["asm"]["dynCall_viiiiijii"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_ifffi = Module["dynCall_ifffi"] = function() {
  return (dynCall_ifffi = Module["dynCall_ifffi"] = Module["asm"]["dynCall_ifffi"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_iffffi = Module["dynCall_iffffi"] = function() {
  return (dynCall_iffffi = Module["dynCall_iffffi"] = Module["asm"]["dynCall_iffffi"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_iiddi = Module["dynCall_iiddi"] = function() {
  return (dynCall_iiddi = Module["dynCall_iiddi"] = Module["asm"]["dynCall_iiddi"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_iiijiii = Module["dynCall_iiijiii"] = function() {
  return (dynCall_iiijiii = Module["dynCall_iiijiii"] = Module["asm"]["dynCall_iiijiii"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_iiifiii = Module["dynCall_iiifiii"] = function() {
  return (dynCall_iiifiii = Module["dynCall_iiifiii"] = Module["asm"]["dynCall_iiifiii"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_iiidiii = Module["dynCall_iiidiii"] = function() {
  return (dynCall_iiidiii = Module["dynCall_iiidiii"] = Module["asm"]["dynCall_iiidiii"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_jd = Module["dynCall_jd"] = function() {
  return (dynCall_jd = Module["dynCall_jd"] = Module["asm"]["dynCall_jd"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_id = Module["dynCall_id"] = function() {
  return (dynCall_id = Module["dynCall_id"] = Module["asm"]["dynCall_id"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_ddd = Module["dynCall_ddd"] = function() {
  return (dynCall_ddd = Module["dynCall_ddd"] = Module["asm"]["dynCall_ddd"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_jf = Module["dynCall_jf"] = function() {
  return (dynCall_jf = Module["dynCall_jf"] = Module["asm"]["dynCall_jf"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_if = Module["dynCall_if"] = function() {
  return (dynCall_if = Module["dynCall_if"] = Module["asm"]["dynCall_if"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_fff = Module["dynCall_fff"] = function() {
  return (dynCall_fff = Module["dynCall_fff"] = Module["asm"]["dynCall_fff"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_dd = Module["dynCall_dd"] = function() {
  return (dynCall_dd = Module["dynCall_dd"] = Module["asm"]["dynCall_dd"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_iiijiiii = Module["dynCall_iiijiiii"] = function() {
  return (dynCall_iiijiiii = Module["dynCall_iiijiiii"] = Module["asm"]["dynCall_iiijiiii"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_iiiijii = Module["dynCall_iiiijii"] = function() {
  return (dynCall_iiiijii = Module["dynCall_iiiijii"] = Module["asm"]["dynCall_iiiijii"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_viiiiiiiiiiiiiiiiii = Module["dynCall_viiiiiiiiiiiiiiiiii"] = function() {
  return (dynCall_viiiiiiiiiiiiiiiiii = Module["dynCall_viiiiiiiiiiiiiiiiii"] = Module["asm"]["dynCall_viiiiiiiiiiiiiiiiii"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_viiijjiii = Module["dynCall_viiijjiii"] = function() {
  return (dynCall_viiijjiii = Module["dynCall_viiijjiii"] = Module["asm"]["dynCall_viiijjiii"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_fi = Module["dynCall_fi"] = function() {
  return (dynCall_fi = Module["dynCall_fi"] = Module["asm"]["dynCall_fi"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_iiiiijii = Module["dynCall_iiiiijii"] = function() {
  return (dynCall_iiiiijii = Module["dynCall_iiiiijii"] = Module["asm"]["dynCall_iiiiijii"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_di = Module["dynCall_di"] = function() {
  return (dynCall_di = Module["dynCall_di"] = Module["asm"]["dynCall_di"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_vidd = Module["dynCall_vidd"] = function() {
  return (dynCall_vidd = Module["dynCall_vidd"] = Module["asm"]["dynCall_vidd"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_vj = Module["dynCall_vj"] = function() {
  return (dynCall_vj = Module["dynCall_vj"] = Module["asm"]["dynCall_vj"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_ij = Module["dynCall_ij"] = function() {
  return (dynCall_ij = Module["dynCall_ij"] = Module["asm"]["dynCall_ij"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_iij = Module["dynCall_iij"] = function() {
  return (dynCall_iij = Module["dynCall_iij"] = Module["asm"]["dynCall_iij"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_jj = Module["dynCall_jj"] = function() {
  return (dynCall_jj = Module["dynCall_jj"] = Module["asm"]["dynCall_jj"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_iiijiiiii = Module["dynCall_iiijiiiii"] = function() {
  return (dynCall_iiijiiiii = Module["dynCall_iiijiiiii"] = Module["asm"]["dynCall_iiijiiiii"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_idddddddddii = Module["dynCall_idddddddddii"] = function() {
  return (dynCall_idddddddddii = Module["dynCall_idddddddddii"] = Module["asm"]["dynCall_idddddddddii"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_j = Module["dynCall_j"] = function() {
  return (dynCall_j = Module["dynCall_j"] = Module["asm"]["dynCall_j"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_vffff = Module["dynCall_vffff"] = function() {
  return (dynCall_vffff = Module["dynCall_vffff"] = Module["asm"]["dynCall_vffff"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_vf = Module["dynCall_vf"] = function() {
  return (dynCall_vf = Module["dynCall_vf"] = Module["asm"]["dynCall_vf"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_vff = Module["dynCall_vff"] = function() {
  return (dynCall_vff = Module["dynCall_vff"] = Module["asm"]["dynCall_vff"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_viif = Module["dynCall_viif"] = function() {
  return (dynCall_viif = Module["dynCall_viif"] = Module["asm"]["dynCall_viif"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_vif = Module["dynCall_vif"] = function() {
  return (dynCall_vif = Module["dynCall_vif"] = Module["asm"]["dynCall_vif"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_viff = Module["dynCall_viff"] = function() {
  return (dynCall_viff = Module["dynCall_viff"] = Module["asm"]["dynCall_viff"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_vifff = Module["dynCall_vifff"] = function() {
  return (dynCall_vifff = Module["dynCall_vifff"] = Module["asm"]["dynCall_vifff"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_viffff = Module["dynCall_viffff"] = function() {
  return (dynCall_viffff = Module["dynCall_viffff"] = Module["asm"]["dynCall_viffff"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_iidiiii = Module["dynCall_iidiiii"] = function() {
  return (dynCall_iidiiii = Module["dynCall_iidiiii"] = Module["asm"]["dynCall_iidiiii"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_viffii = Module["dynCall_viffii"] = function() {
  return (dynCall_viffii = Module["dynCall_viffii"] = Module["asm"]["dynCall_viffii"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_iijj = Module["dynCall_iijj"] = function() {
  return (dynCall_iijj = Module["dynCall_iijj"] = Module["asm"]["dynCall_iijj"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_iiiij = Module["dynCall_iiiij"] = function() {
  return (dynCall_iiiij = Module["dynCall_iiiij"] = Module["asm"]["dynCall_iiiij"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_viij = Module["dynCall_viij"] = function() {
  return (dynCall_viij = Module["dynCall_viij"] = Module["asm"]["dynCall_viij"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_ff = Module["dynCall_ff"] = function() {
  return (dynCall_ff = Module["dynCall_ff"] = Module["asm"]["dynCall_ff"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_jijj = Module["dynCall_jijj"] = function() {
  return (dynCall_jijj = Module["dynCall_jijj"] = Module["asm"]["dynCall_jijj"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_jij = Module["dynCall_jij"] = function() {
  return (dynCall_jij = Module["dynCall_jij"] = Module["asm"]["dynCall_jij"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_viiiiiiiiiiiiii = Module["dynCall_viiiiiiiiiiiiii"] = function() {
  return (dynCall_viiiiiiiiiiiiii = Module["dynCall_viiiiiiiiiiiiii"] = Module["asm"]["dynCall_viiiiiiiiiiiiii"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_viiiiiiiiiiiiiii = Module["dynCall_viiiiiiiiiiiiiii"] = function() {
  return (dynCall_viiiiiiiiiiiiiii = Module["dynCall_viiiiiiiiiiiiiii"] = Module["asm"]["dynCall_viiiiiiiiiiiiiii"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_viiiiiiiiiiiiiiii = Module["dynCall_viiiiiiiiiiiiiiii"] = function() {
  return (dynCall_viiiiiiiiiiiiiiii = Module["dynCall_viiiiiiiiiiiiiiii"] = Module["asm"]["dynCall_viiiiiiiiiiiiiiii"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_viiiiiiiiiiiiiiiii = Module["dynCall_viiiiiiiiiiiiiiiii"] = function() {
  return (dynCall_viiiiiiiiiiiiiiiii = Module["dynCall_viiiiiiiiiiiiiiiii"] = Module["asm"]["dynCall_viiiiiiiiiiiiiiiii"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_viiiiiiiiiiiiiiiiiii = Module["dynCall_viiiiiiiiiiiiiiiiiii"] = function() {
  return (dynCall_viiiiiiiiiiiiiiiiiii = Module["dynCall_viiiiiiiiiiiiiiiiiii"] = Module["asm"]["dynCall_viiiiiiiiiiiiiiiiiii"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_viiiiiiiiiiiiiiiiiiii = Module["dynCall_viiiiiiiiiiiiiiiiiiii"] = function() {
  return (dynCall_viiiiiiiiiiiiiiiiiiii = Module["dynCall_viiiiiiiiiiiiiiiiiiii"] = Module["asm"]["dynCall_viiiiiiiiiiiiiiiiiiii"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_viiiiiiiiiiiiiiiiiiiii = Module["dynCall_viiiiiiiiiiiiiiiiiiiii"] = function() {
  return (dynCall_viiiiiiiiiiiiiiiiiiiii = Module["dynCall_viiiiiiiiiiiiiiiiiiiii"] = Module["asm"]["dynCall_viiiiiiiiiiiiiiiiiiiii"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_viiiiiiiiiiiiiiiiiiiiii = Module["dynCall_viiiiiiiiiiiiiiiiiiiiii"] = function() {
  return (dynCall_viiiiiiiiiiiiiiiiiiiiii = Module["dynCall_viiiiiiiiiiiiiiiiiiiiii"] = Module["asm"]["dynCall_viiiiiiiiiiiiiiiiiiiiii"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_viiiiiiiiiiiiiiiiiiiiiii = Module["dynCall_viiiiiiiiiiiiiiiiiiiiiii"] = function() {
  return (dynCall_viiiiiiiiiiiiiiiiiiiiiii = Module["dynCall_viiiiiiiiiiiiiiiiiiiiiii"] = Module["asm"]["dynCall_viiiiiiiiiiiiiiiiiiiiiii"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_viiiiiiiiiiiiiiiiiiiiiiii = Module["dynCall_viiiiiiiiiiiiiiiiiiiiiiii"] = function() {
  return (dynCall_viiiiiiiiiiiiiiiiiiiiiiii = Module["dynCall_viiiiiiiiiiiiiiiiiiiiiiii"] = Module["asm"]["dynCall_viiiiiiiiiiiiiiiiiiiiiiii"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_viiiiiiiiiiiiiiiiiiiiiiiii = Module["dynCall_viiiiiiiiiiiiiiiiiiiiiiiii"] = function() {
  return (dynCall_viiiiiiiiiiiiiiiiiiiiiiiii = Module["dynCall_viiiiiiiiiiiiiiiiiiiiiiiii"] = Module["asm"]["dynCall_viiiiiiiiiiiiiiiiiiiiiiiii"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_viiiiiiiiiiiiiiiiiiiiiiiiii = Module["dynCall_viiiiiiiiiiiiiiiiiiiiiiiiii"] = function() {
  return (dynCall_viiiiiiiiiiiiiiiiiiiiiiiiii = Module["dynCall_viiiiiiiiiiiiiiiiiiiiiiiiii"] = Module["asm"]["dynCall_viiiiiiiiiiiiiiiiiiiiiiiiii"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_viiiiiiiiiiiiiiiiiiiiiiiiiii = Module["dynCall_viiiiiiiiiiiiiiiiiiiiiiiiiii"] = function() {
  return (dynCall_viiiiiiiiiiiiiiiiiiiiiiiiiii = Module["dynCall_viiiiiiiiiiiiiiiiiiiiiiiiiii"] = Module["asm"]["dynCall_viiiiiiiiiiiiiiiiiiiiiiiiiii"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_viiiiiiiiiiiiiiiiiiiiiiiiiiii = Module["dynCall_viiiiiiiiiiiiiiiiiiiiiiiiiiii"] = function() {
  return (dynCall_viiiiiiiiiiiiiiiiiiiiiiiiiiii = Module["dynCall_viiiiiiiiiiiiiiiiiiiiiiiiiiii"] = Module["asm"]["dynCall_viiiiiiiiiiiiiiiiiiiiiiiiiiii"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_viiiiiiiiiiiiiiiiiiiiiiiiiiiii = Module["dynCall_viiiiiiiiiiiiiiiiiiiiiiiiiiiii"] = function() {
  return (dynCall_viiiiiiiiiiiiiiiiiiiiiiiiiiiii = Module["dynCall_viiiiiiiiiiiiiiiiiiiiiiiiiiiii"] = Module["asm"]["dynCall_viiiiiiiiiiiiiiiiiiiiiiiiiiiii"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_viiiiiiiiiiiiiiiiiiiiiiiiiiiiii = Module["dynCall_viiiiiiiiiiiiiiiiiiiiiiiiiiiiii"] = function() {
  return (dynCall_viiiiiiiiiiiiiiiiiiiiiiiiiiiiii = Module["dynCall_viiiiiiiiiiiiiiiiiiiiiiiiiiiiii"] = Module["asm"]["dynCall_viiiiiiiiiiiiiiiiiiiiiiiiiiiiii"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_viiiiiiiiiiiiiiiiiiiiiiiiiiiiiii = Module["dynCall_viiiiiiiiiiiiiiiiiiiiiiiiiiiiiii"] = function() {
  return (dynCall_viiiiiiiiiiiiiiiiiiiiiiiiiiiiiii = Module["dynCall_viiiiiiiiiiiiiiiiiiiiiiiiiiiiiii"] = Module["asm"]["dynCall_viiiiiiiiiiiiiiiiiiiiiiiiiiiiiii"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_viiiiiiiiiiiiiiiiiiiiiiiiiiiiiiii = Module["dynCall_viiiiiiiiiiiiiiiiiiiiiiiiiiiiiiii"] = function() {
  return (dynCall_viiiiiiiiiiiiiiiiiiiiiiiiiiiiiiii = Module["dynCall_viiiiiiiiiiiiiiiiiiiiiiiiiiiiiiii"] = Module["asm"]["dynCall_viiiiiiiiiiiiiiiiiiiiiiiiiiiiiiii"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_viiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiii = Module["dynCall_viiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiii"] = function() {
  return (dynCall_viiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiii = Module["dynCall_viiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiii"] = Module["asm"]["dynCall_viiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiii"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_viiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiii = Module["dynCall_viiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiii"] = function() {
  return (dynCall_viiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiii = Module["dynCall_viiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiii"] = Module["asm"]["dynCall_viiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiii"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_viiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiii = Module["dynCall_viiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiii"] = function() {
  return (dynCall_viiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiii = Module["dynCall_viiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiii"] = Module["asm"]["dynCall_viiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiii"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_viiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiii = Module["dynCall_viiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiii"] = function() {
  return (dynCall_viiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiii = Module["dynCall_viiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiii"] = Module["asm"]["dynCall_viiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiii"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_viiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiii = Module["dynCall_viiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiii"] = function() {
  return (dynCall_viiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiii = Module["dynCall_viiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiii"] = Module["asm"]["dynCall_viiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiii"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_viiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiii = Module["dynCall_viiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiii"] = function() {
  return (dynCall_viiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiii = Module["dynCall_viiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiii"] = Module["asm"]["dynCall_viiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiii"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_viiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiii = Module["dynCall_viiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiii"] = function() {
  return (dynCall_viiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiii = Module["dynCall_viiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiii"] = Module["asm"]["dynCall_viiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiii"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_viiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiii = Module["dynCall_viiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiii"] = function() {
  return (dynCall_viiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiii = Module["dynCall_viiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiii"] = Module["asm"]["dynCall_viiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiii"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_viiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiii = Module["dynCall_viiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiii"] = function() {
  return (dynCall_viiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiii = Module["dynCall_viiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiii"] = Module["asm"]["dynCall_viiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiii"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var dynCall_viiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiii = Module["dynCall_viiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiii"] = function() {
  return (dynCall_viiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiii = Module["dynCall_viiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiii"] = Module["asm"]["dynCall_viiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiii"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var _asyncify_start_unwind = Module["_asyncify_start_unwind"] = function() {
  return (_asyncify_start_unwind = Module["_asyncify_start_unwind"] = Module["asm"]["asyncify_start_unwind"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var _asyncify_stop_unwind = Module["_asyncify_stop_unwind"] = function() {
  return (_asyncify_stop_unwind = Module["_asyncify_stop_unwind"] = Module["asm"]["asyncify_stop_unwind"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var _asyncify_start_rewind = Module["_asyncify_start_rewind"] = function() {
  return (_asyncify_start_rewind = Module["_asyncify_start_rewind"] = Module["asm"]["asyncify_start_rewind"]).apply(null, arguments);
};

/** @type {function(...*):?} */
var _asyncify_stop_rewind = Module["_asyncify_stop_rewind"] = function() {
  return (_asyncify_stop_rewind = Module["_asyncify_stop_rewind"] = Module["asm"]["asyncify_stop_rewind"]).apply(null, arguments);
};


function invoke_ii(index,a1) {
  var sp = stackSave();
  try {
    return dynCall_ii(index,a1);
  } catch(e) {
    stackRestore(sp);
    if (e !== e+0) throw e;
    _setThrew(1, 0);
  }
}

function invoke_v(index) {
  var sp = stackSave();
  try {
    dynCall_v(index);
  } catch(e) {
    stackRestore(sp);
    if (e !== e+0) throw e;
    _setThrew(1, 0);
  }
}

function invoke_iii(index,a1,a2) {
  var sp = stackSave();
  try {
    return dynCall_iii(index,a1,a2);
  } catch(e) {
    stackRestore(sp);
    if (e !== e+0) throw e;
    _setThrew(1, 0);
  }
}

function invoke_iiii(index,a1,a2,a3) {
  var sp = stackSave();
  try {
    return dynCall_iiii(index,a1,a2,a3);
  } catch(e) {
    stackRestore(sp);
    if (e !== e+0) throw e;
    _setThrew(1, 0);
  }
}

function invoke_viii(index,a1,a2,a3) {
  var sp = stackSave();
  try {
    dynCall_viii(index,a1,a2,a3);
  } catch(e) {
    stackRestore(sp);
    if (e !== e+0) throw e;
    _setThrew(1, 0);
  }
}

function invoke_iiiiii(index,a1,a2,a3,a4,a5) {
  var sp = stackSave();
  try {
    return dynCall_iiiiii(index,a1,a2,a3,a4,a5);
  } catch(e) {
    stackRestore(sp);
    if (e !== e+0) throw e;
    _setThrew(1, 0);
  }
}

function invoke_vii(index,a1,a2) {
  var sp = stackSave();
  try {
    dynCall_vii(index,a1,a2);
  } catch(e) {
    stackRestore(sp);
    if (e !== e+0) throw e;
    _setThrew(1, 0);
  }
}

function invoke_viiii(index,a1,a2,a3,a4) {
  var sp = stackSave();
  try {
    dynCall_viiii(index,a1,a2,a3,a4);
  } catch(e) {
    stackRestore(sp);
    if (e !== e+0) throw e;
    _setThrew(1, 0);
  }
}

function invoke_vi(index,a1) {
  var sp = stackSave();
  try {
    dynCall_vi(index,a1);
  } catch(e) {
    stackRestore(sp);
    if (e !== e+0) throw e;
    _setThrew(1, 0);
  }
}

function invoke_viiiii(index,a1,a2,a3,a4,a5) {
  var sp = stackSave();
  try {
    dynCall_viiiii(index,a1,a2,a3,a4,a5);
  } catch(e) {
    stackRestore(sp);
    if (e !== e+0) throw e;
    _setThrew(1, 0);
  }
}

function invoke_viiiiiiiiiii(index,a1,a2,a3,a4,a5,a6,a7,a8,a9,a10,a11) {
  var sp = stackSave();
  try {
    dynCall_viiiiiiiiiii(index,a1,a2,a3,a4,a5,a6,a7,a8,a9,a10,a11);
  } catch(e) {
    stackRestore(sp);
    if (e !== e+0) throw e;
    _setThrew(1, 0);
  }
}

function invoke_iiiii(index,a1,a2,a3,a4) {
  var sp = stackSave();
  try {
    return dynCall_iiiii(index,a1,a2,a3,a4);
  } catch(e) {
    stackRestore(sp);
    if (e !== e+0) throw e;
    _setThrew(1, 0);
  }
}

function invoke_iiiiiiii(index,a1,a2,a3,a4,a5,a6,a7) {
  var sp = stackSave();
  try {
    return dynCall_iiiiiiii(index,a1,a2,a3,a4,a5,a6,a7);
  } catch(e) {
    stackRestore(sp);
    if (e !== e+0) throw e;
    _setThrew(1, 0);
  }
}

function invoke_iiiiiii(index,a1,a2,a3,a4,a5,a6) {
  var sp = stackSave();
  try {
    return dynCall_iiiiiii(index,a1,a2,a3,a4,a5,a6);
  } catch(e) {
    stackRestore(sp);
    if (e !== e+0) throw e;
    _setThrew(1, 0);
  }
}

function invoke_viiiiii(index,a1,a2,a3,a4,a5,a6) {
  var sp = stackSave();
  try {
    dynCall_viiiiii(index,a1,a2,a3,a4,a5,a6);
  } catch(e) {
    stackRestore(sp);
    if (e !== e+0) throw e;
    _setThrew(1, 0);
  }
}

function invoke_viiiiiii(index,a1,a2,a3,a4,a5,a6,a7) {
  var sp = stackSave();
  try {
    dynCall_viiiiiii(index,a1,a2,a3,a4,a5,a6,a7);
  } catch(e) {
    stackRestore(sp);
    if (e !== e+0) throw e;
    _setThrew(1, 0);
  }
}

function invoke_viiiiiiiii(index,a1,a2,a3,a4,a5,a6,a7,a8,a9) {
  var sp = stackSave();
  try {
    dynCall_viiiiiiiii(index,a1,a2,a3,a4,a5,a6,a7,a8,a9);
  } catch(e) {
    stackRestore(sp);
    if (e !== e+0) throw e;
    _setThrew(1, 0);
  }
}

function invoke_iiiiiiiii(index,a1,a2,a3,a4,a5,a6,a7,a8) {
  var sp = stackSave();
  try {
    return dynCall_iiiiiiiii(index,a1,a2,a3,a4,a5,a6,a7,a8);
  } catch(e) {
    stackRestore(sp);
    if (e !== e+0) throw e;
    _setThrew(1, 0);
  }
}

function invoke_viiiiiiii(index,a1,a2,a3,a4,a5,a6,a7,a8) {
  var sp = stackSave();
  try {
    dynCall_viiiiiiii(index,a1,a2,a3,a4,a5,a6,a7,a8);
  } catch(e) {
    stackRestore(sp);
    if (e !== e+0) throw e;
    _setThrew(1, 0);
  }
}

function invoke_i(index) {
  var sp = stackSave();
  try {
    return dynCall_i(index);
  } catch(e) {
    stackRestore(sp);
    if (e !== e+0) throw e;
    _setThrew(1, 0);
  }
}

function invoke_viiiiiiiiii(index,a1,a2,a3,a4,a5,a6,a7,a8,a9,a10) {
  var sp = stackSave();
  try {
    dynCall_viiiiiiiiii(index,a1,a2,a3,a4,a5,a6,a7,a8,a9,a10);
  } catch(e) {
    stackRestore(sp);
    if (e !== e+0) throw e;
    _setThrew(1, 0);
  }
}

function invoke_viiiiiiiiiiii(index,a1,a2,a3,a4,a5,a6,a7,a8,a9,a10,a11,a12) {
  var sp = stackSave();
  try {
    dynCall_viiiiiiiiiiii(index,a1,a2,a3,a4,a5,a6,a7,a8,a9,a10,a11,a12);
  } catch(e) {
    stackRestore(sp);
    if (e !== e+0) throw e;
    _setThrew(1, 0);
  }
}

function invoke_viiiiiiiiiiiii(index,a1,a2,a3,a4,a5,a6,a7,a8,a9,a10,a11,a12,a13) {
  var sp = stackSave();
  try {
    dynCall_viiiiiiiiiiiii(index,a1,a2,a3,a4,a5,a6,a7,a8,a9,a10,a11,a12,a13);
  } catch(e) {
    stackRestore(sp);
    if (e !== e+0) throw e;
    _setThrew(1, 0);
  }
}

function invoke_viiiiiiiiiiiiii(index,a1,a2,a3,a4,a5,a6,a7,a8,a9,a10,a11,a12,a13,a14) {
  var sp = stackSave();
  try {
    dynCall_viiiiiiiiiiiiii(index,a1,a2,a3,a4,a5,a6,a7,a8,a9,a10,a11,a12,a13,a14);
  } catch(e) {
    stackRestore(sp);
    if (e !== e+0) throw e;
    _setThrew(1, 0);
  }
}

function invoke_viiiiiiiiiiiiiii(index,a1,a2,a3,a4,a5,a6,a7,a8,a9,a10,a11,a12,a13,a14,a15) {
  var sp = stackSave();
  try {
    dynCall_viiiiiiiiiiiiiii(index,a1,a2,a3,a4,a5,a6,a7,a8,a9,a10,a11,a12,a13,a14,a15);
  } catch(e) {
    stackRestore(sp);
    if (e !== e+0) throw e;
    _setThrew(1, 0);
  }
}

function invoke_viiiiiiiiiiiiiiii(index,a1,a2,a3,a4,a5,a6,a7,a8,a9,a10,a11,a12,a13,a14,a15,a16) {
  var sp = stackSave();
  try {
    dynCall_viiiiiiiiiiiiiiii(index,a1,a2,a3,a4,a5,a6,a7,a8,a9,a10,a11,a12,a13,a14,a15,a16);
  } catch(e) {
    stackRestore(sp);
    if (e !== e+0) throw e;
    _setThrew(1, 0);
  }
}

function invoke_viiiiiiiiiiiiiiiii(index,a1,a2,a3,a4,a5,a6,a7,a8,a9,a10,a11,a12,a13,a14,a15,a16,a17) {
  var sp = stackSave();
  try {
    dynCall_viiiiiiiiiiiiiiiii(index,a1,a2,a3,a4,a5,a6,a7,a8,a9,a10,a11,a12,a13,a14,a15,a16,a17);
  } catch(e) {
    stackRestore(sp);
    if (e !== e+0) throw e;
    _setThrew(1, 0);
  }
}

function invoke_viiiiiiiiiiiiiiiiii(index,a1,a2,a3,a4,a5,a6,a7,a8,a9,a10,a11,a12,a13,a14,a15,a16,a17,a18) {
  var sp = stackSave();
  try {
    dynCall_viiiiiiiiiiiiiiiiii(index,a1,a2,a3,a4,a5,a6,a7,a8,a9,a10,a11,a12,a13,a14,a15,a16,a17,a18);
  } catch(e) {
    stackRestore(sp);
    if (e !== e+0) throw e;
    _setThrew(1, 0);
  }
}

function invoke_viiiiiiiiiiiiiiiiiii(index,a1,a2,a3,a4,a5,a6,a7,a8,a9,a10,a11,a12,a13,a14,a15,a16,a17,a18,a19) {
  var sp = stackSave();
  try {
    dynCall_viiiiiiiiiiiiiiiiiii(index,a1,a2,a3,a4,a5,a6,a7,a8,a9,a10,a11,a12,a13,a14,a15,a16,a17,a18,a19);
  } catch(e) {
    stackRestore(sp);
    if (e !== e+0) throw e;
    _setThrew(1, 0);
  }
}

function invoke_viiiiiiiiiiiiiiiiiiii(index,a1,a2,a3,a4,a5,a6,a7,a8,a9,a10,a11,a12,a13,a14,a15,a16,a17,a18,a19,a20) {
  var sp = stackSave();
  try {
    dynCall_viiiiiiiiiiiiiiiiiiii(index,a1,a2,a3,a4,a5,a6,a7,a8,a9,a10,a11,a12,a13,a14,a15,a16,a17,a18,a19,a20);
  } catch(e) {
    stackRestore(sp);
    if (e !== e+0) throw e;
    _setThrew(1, 0);
  }
}

function invoke_viiiiiiiiiiiiiiiiiiiii(index,a1,a2,a3,a4,a5,a6,a7,a8,a9,a10,a11,a12,a13,a14,a15,a16,a17,a18,a19,a20,a21) {
  var sp = stackSave();
  try {
    dynCall_viiiiiiiiiiiiiiiiiiiii(index,a1,a2,a3,a4,a5,a6,a7,a8,a9,a10,a11,a12,a13,a14,a15,a16,a17,a18,a19,a20,a21);
  } catch(e) {
    stackRestore(sp);
    if (e !== e+0) throw e;
    _setThrew(1, 0);
  }
}

function invoke_viiiiiiiiiiiiiiiiiiiiii(index,a1,a2,a3,a4,a5,a6,a7,a8,a9,a10,a11,a12,a13,a14,a15,a16,a17,a18,a19,a20,a21,a22) {
  var sp = stackSave();
  try {
    dynCall_viiiiiiiiiiiiiiiiiiiiii(index,a1,a2,a3,a4,a5,a6,a7,a8,a9,a10,a11,a12,a13,a14,a15,a16,a17,a18,a19,a20,a21,a22);
  } catch(e) {
    stackRestore(sp);
    if (e !== e+0) throw e;
    _setThrew(1, 0);
  }
}

function invoke_viiiiiiiiiiiiiiiiiiiiiii(index,a1,a2,a3,a4,a5,a6,a7,a8,a9,a10,a11,a12,a13,a14,a15,a16,a17,a18,a19,a20,a21,a22,a23) {
  var sp = stackSave();
  try {
    dynCall_viiiiiiiiiiiiiiiiiiiiiii(index,a1,a2,a3,a4,a5,a6,a7,a8,a9,a10,a11,a12,a13,a14,a15,a16,a17,a18,a19,a20,a21,a22,a23);
  } catch(e) {
    stackRestore(sp);
    if (e !== e+0) throw e;
    _setThrew(1, 0);
  }
}

function invoke_viiiiiiiiiiiiiiiiiiiiiiii(index,a1,a2,a3,a4,a5,a6,a7,a8,a9,a10,a11,a12,a13,a14,a15,a16,a17,a18,a19,a20,a21,a22,a23,a24) {
  var sp = stackSave();
  try {
    dynCall_viiiiiiiiiiiiiiiiiiiiiiii(index,a1,a2,a3,a4,a5,a6,a7,a8,a9,a10,a11,a12,a13,a14,a15,a16,a17,a18,a19,a20,a21,a22,a23,a24);
  } catch(e) {
    stackRestore(sp);
    if (e !== e+0) throw e;
    _setThrew(1, 0);
  }
}

function invoke_viiiiiiiiiiiiiiiiiiiiiiiii(index,a1,a2,a3,a4,a5,a6,a7,a8,a9,a10,a11,a12,a13,a14,a15,a16,a17,a18,a19,a20,a21,a22,a23,a24,a25) {
  var sp = stackSave();
  try {
    dynCall_viiiiiiiiiiiiiiiiiiiiiiiii(index,a1,a2,a3,a4,a5,a6,a7,a8,a9,a10,a11,a12,a13,a14,a15,a16,a17,a18,a19,a20,a21,a22,a23,a24,a25);
  } catch(e) {
    stackRestore(sp);
    if (e !== e+0) throw e;
    _setThrew(1, 0);
  }
}

function invoke_viiiiiiiiiiiiiiiiiiiiiiiiii(index,a1,a2,a3,a4,a5,a6,a7,a8,a9,a10,a11,a12,a13,a14,a15,a16,a17,a18,a19,a20,a21,a22,a23,a24,a25,a26) {
  var sp = stackSave();
  try {
    dynCall_viiiiiiiiiiiiiiiiiiiiiiiiii(index,a1,a2,a3,a4,a5,a6,a7,a8,a9,a10,a11,a12,a13,a14,a15,a16,a17,a18,a19,a20,a21,a22,a23,a24,a25,a26);
  } catch(e) {
    stackRestore(sp);
    if (e !== e+0) throw e;
    _setThrew(1, 0);
  }
}

function invoke_viiiiiiiiiiiiiiiiiiiiiiiiiii(index,a1,a2,a3,a4,a5,a6,a7,a8,a9,a10,a11,a12,a13,a14,a15,a16,a17,a18,a19,a20,a21,a22,a23,a24,a25,a26,a27) {
  var sp = stackSave();
  try {
    dynCall_viiiiiiiiiiiiiiiiiiiiiiiiiii(index,a1,a2,a3,a4,a5,a6,a7,a8,a9,a10,a11,a12,a13,a14,a15,a16,a17,a18,a19,a20,a21,a22,a23,a24,a25,a26,a27);
  } catch(e) {
    stackRestore(sp);
    if (e !== e+0) throw e;
    _setThrew(1, 0);
  }
}

function invoke_viiiiiiiiiiiiiiiiiiiiiiiiiiii(index,a1,a2,a3,a4,a5,a6,a7,a8,a9,a10,a11,a12,a13,a14,a15,a16,a17,a18,a19,a20,a21,a22,a23,a24,a25,a26,a27,a28) {
  var sp = stackSave();
  try {
    dynCall_viiiiiiiiiiiiiiiiiiiiiiiiiiii(index,a1,a2,a3,a4,a5,a6,a7,a8,a9,a10,a11,a12,a13,a14,a15,a16,a17,a18,a19,a20,a21,a22,a23,a24,a25,a26,a27,a28);
  } catch(e) {
    stackRestore(sp);
    if (e !== e+0) throw e;
    _setThrew(1, 0);
  }
}

function invoke_viiiiiiiiiiiiiiiiiiiiiiiiiiiii(index,a1,a2,a3,a4,a5,a6,a7,a8,a9,a10,a11,a12,a13,a14,a15,a16,a17,a18,a19,a20,a21,a22,a23,a24,a25,a26,a27,a28,a29) {
  var sp = stackSave();
  try {
    dynCall_viiiiiiiiiiiiiiiiiiiiiiiiiiiii(index,a1,a2,a3,a4,a5,a6,a7,a8,a9,a10,a11,a12,a13,a14,a15,a16,a17,a18,a19,a20,a21,a22,a23,a24,a25,a26,a27,a28,a29);
  } catch(e) {
    stackRestore(sp);
    if (e !== e+0) throw e;
    _setThrew(1, 0);
  }
}

function invoke_viiiiiiiiiiiiiiiiiiiiiiiiiiiiii(index,a1,a2,a3,a4,a5,a6,a7,a8,a9,a10,a11,a12,a13,a14,a15,a16,a17,a18,a19,a20,a21,a22,a23,a24,a25,a26,a27,a28,a29,a30) {
  var sp = stackSave();
  try {
    dynCall_viiiiiiiiiiiiiiiiiiiiiiiiiiiiii(index,a1,a2,a3,a4,a5,a6,a7,a8,a9,a10,a11,a12,a13,a14,a15,a16,a17,a18,a19,a20,a21,a22,a23,a24,a25,a26,a27,a28,a29,a30);
  } catch(e) {
    stackRestore(sp);
    if (e !== e+0) throw e;
    _setThrew(1, 0);
  }
}

function invoke_viiiiiiiiiiiiiiiiiiiiiiiiiiiiiii(index,a1,a2,a3,a4,a5,a6,a7,a8,a9,a10,a11,a12,a13,a14,a15,a16,a17,a18,a19,a20,a21,a22,a23,a24,a25,a26,a27,a28,a29,a30,a31) {
  var sp = stackSave();
  try {
    dynCall_viiiiiiiiiiiiiiiiiiiiiiiiiiiiiii(index,a1,a2,a3,a4,a5,a6,a7,a8,a9,a10,a11,a12,a13,a14,a15,a16,a17,a18,a19,a20,a21,a22,a23,a24,a25,a26,a27,a28,a29,a30,a31);
  } catch(e) {
    stackRestore(sp);
    if (e !== e+0) throw e;
    _setThrew(1, 0);
  }
}

function invoke_viiiiiiiiiiiiiiiiiiiiiiiiiiiiiiii(index,a1,a2,a3,a4,a5,a6,a7,a8,a9,a10,a11,a12,a13,a14,a15,a16,a17,a18,a19,a20,a21,a22,a23,a24,a25,a26,a27,a28,a29,a30,a31,a32) {
  var sp = stackSave();
  try {
    dynCall_viiiiiiiiiiiiiiiiiiiiiiiiiiiiiiii(index,a1,a2,a3,a4,a5,a6,a7,a8,a9,a10,a11,a12,a13,a14,a15,a16,a17,a18,a19,a20,a21,a22,a23,a24,a25,a26,a27,a28,a29,a30,a31,a32);
  } catch(e) {
    stackRestore(sp);
    if (e !== e+0) throw e;
    _setThrew(1, 0);
  }
}

function invoke_viiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiii(index,a1,a2,a3,a4,a5,a6,a7,a8,a9,a10,a11,a12,a13,a14,a15,a16,a17,a18,a19,a20,a21,a22,a23,a24,a25,a26,a27,a28,a29,a30,a31,a32,a33) {
  var sp = stackSave();
  try {
    dynCall_viiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiii(index,a1,a2,a3,a4,a5,a6,a7,a8,a9,a10,a11,a12,a13,a14,a15,a16,a17,a18,a19,a20,a21,a22,a23,a24,a25,a26,a27,a28,a29,a30,a31,a32,a33);
  } catch(e) {
    stackRestore(sp);
    if (e !== e+0) throw e;
    _setThrew(1, 0);
  }
}

function invoke_viiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiii(index,a1,a2,a3,a4,a5,a6,a7,a8,a9,a10,a11,a12,a13,a14,a15,a16,a17,a18,a19,a20,a21,a22,a23,a24,a25,a26,a27,a28,a29,a30,a31,a32,a33,a34) {
  var sp = stackSave();
  try {
    dynCall_viiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiii(index,a1,a2,a3,a4,a5,a6,a7,a8,a9,a10,a11,a12,a13,a14,a15,a16,a17,a18,a19,a20,a21,a22,a23,a24,a25,a26,a27,a28,a29,a30,a31,a32,a33,a34);
  } catch(e) {
    stackRestore(sp);
    if (e !== e+0) throw e;
    _setThrew(1, 0);
  }
}

function invoke_viiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiii(index,a1,a2,a3,a4,a5,a6,a7,a8,a9,a10,a11,a12,a13,a14,a15,a16,a17,a18,a19,a20,a21,a22,a23,a24,a25,a26,a27,a28,a29,a30,a31,a32,a33,a34,a35) {
  var sp = stackSave();
  try {
    dynCall_viiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiii(index,a1,a2,a3,a4,a5,a6,a7,a8,a9,a10,a11,a12,a13,a14,a15,a16,a17,a18,a19,a20,a21,a22,a23,a24,a25,a26,a27,a28,a29,a30,a31,a32,a33,a34,a35);
  } catch(e) {
    stackRestore(sp);
    if (e !== e+0) throw e;
    _setThrew(1, 0);
  }
}

function invoke_viiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiii(index,a1,a2,a3,a4,a5,a6,a7,a8,a9,a10,a11,a12,a13,a14,a15,a16,a17,a18,a19,a20,a21,a22,a23,a24,a25,a26,a27,a28,a29,a30,a31,a32,a33,a34,a35,a36) {
  var sp = stackSave();
  try {
    dynCall_viiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiii(index,a1,a2,a3,a4,a5,a6,a7,a8,a9,a10,a11,a12,a13,a14,a15,a16,a17,a18,a19,a20,a21,a22,a23,a24,a25,a26,a27,a28,a29,a30,a31,a32,a33,a34,a35,a36);
  } catch(e) {
    stackRestore(sp);
    if (e !== e+0) throw e;
    _setThrew(1, 0);
  }
}

function invoke_viiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiii(index,a1,a2,a3,a4,a5,a6,a7,a8,a9,a10,a11,a12,a13,a14,a15,a16,a17,a18,a19,a20,a21,a22,a23,a24,a25,a26,a27,a28,a29,a30,a31,a32,a33,a34,a35,a36,a37) {
  var sp = stackSave();
  try {
    dynCall_viiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiii(index,a1,a2,a3,a4,a5,a6,a7,a8,a9,a10,a11,a12,a13,a14,a15,a16,a17,a18,a19,a20,a21,a22,a23,a24,a25,a26,a27,a28,a29,a30,a31,a32,a33,a34,a35,a36,a37);
  } catch(e) {
    stackRestore(sp);
    if (e !== e+0) throw e;
    _setThrew(1, 0);
  }
}

function invoke_viiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiii(index,a1,a2,a3,a4,a5,a6,a7,a8,a9,a10,a11,a12,a13,a14,a15,a16,a17,a18,a19,a20,a21,a22,a23,a24,a25,a26,a27,a28,a29,a30,a31,a32,a33,a34,a35,a36,a37,a38) {
  var sp = stackSave();
  try {
    dynCall_viiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiii(index,a1,a2,a3,a4,a5,a6,a7,a8,a9,a10,a11,a12,a13,a14,a15,a16,a17,a18,a19,a20,a21,a22,a23,a24,a25,a26,a27,a28,a29,a30,a31,a32,a33,a34,a35,a36,a37,a38);
  } catch(e) {
    stackRestore(sp);
    if (e !== e+0) throw e;
    _setThrew(1, 0);
  }
}

function invoke_viiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiii(index,a1,a2,a3,a4,a5,a6,a7,a8,a9,a10,a11,a12,a13,a14,a15,a16,a17,a18,a19,a20,a21,a22,a23,a24,a25,a26,a27,a28,a29,a30,a31,a32,a33,a34,a35,a36,a37,a38,a39) {
  var sp = stackSave();
  try {
    dynCall_viiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiii(index,a1,a2,a3,a4,a5,a6,a7,a8,a9,a10,a11,a12,a13,a14,a15,a16,a17,a18,a19,a20,a21,a22,a23,a24,a25,a26,a27,a28,a29,a30,a31,a32,a33,a34,a35,a36,a37,a38,a39);
  } catch(e) {
    stackRestore(sp);
    if (e !== e+0) throw e;
    _setThrew(1, 0);
  }
}

function invoke_viiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiii(index,a1,a2,a3,a4,a5,a6,a7,a8,a9,a10,a11,a12,a13,a14,a15,a16,a17,a18,a19,a20,a21,a22,a23,a24,a25,a26,a27,a28,a29,a30,a31,a32,a33,a34,a35,a36,a37,a38,a39,a40) {
  var sp = stackSave();
  try {
    dynCall_viiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiii(index,a1,a2,a3,a4,a5,a6,a7,a8,a9,a10,a11,a12,a13,a14,a15,a16,a17,a18,a19,a20,a21,a22,a23,a24,a25,a26,a27,a28,a29,a30,a31,a32,a33,a34,a35,a36,a37,a38,a39,a40);
  } catch(e) {
    stackRestore(sp);
    if (e !== e+0) throw e;
    _setThrew(1, 0);
  }
}

function invoke_viiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiii(index,a1,a2,a3,a4,a5,a6,a7,a8,a9,a10,a11,a12,a13,a14,a15,a16,a17,a18,a19,a20,a21,a22,a23,a24,a25,a26,a27,a28,a29,a30,a31,a32,a33,a34,a35,a36,a37,a38,a39,a40,a41) {
  var sp = stackSave();
  try {
    dynCall_viiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiii(index,a1,a2,a3,a4,a5,a6,a7,a8,a9,a10,a11,a12,a13,a14,a15,a16,a17,a18,a19,a20,a21,a22,a23,a24,a25,a26,a27,a28,a29,a30,a31,a32,a33,a34,a35,a36,a37,a38,a39,a40,a41);
  } catch(e) {
    stackRestore(sp);
    if (e !== e+0) throw e;
    _setThrew(1, 0);
  }
}

function invoke_viiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiii(index,a1,a2,a3,a4,a5,a6,a7,a8,a9,a10,a11,a12,a13,a14,a15,a16,a17,a18,a19,a20,a21,a22,a23,a24,a25,a26,a27,a28,a29,a30,a31,a32,a33,a34,a35,a36,a37,a38,a39,a40,a41,a42) {
  var sp = stackSave();
  try {
    dynCall_viiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiii(index,a1,a2,a3,a4,a5,a6,a7,a8,a9,a10,a11,a12,a13,a14,a15,a16,a17,a18,a19,a20,a21,a22,a23,a24,a25,a26,a27,a28,a29,a30,a31,a32,a33,a34,a35,a36,a37,a38,a39,a40,a41,a42);
  } catch(e) {
    stackRestore(sp);
    if (e !== e+0) throw e;
    _setThrew(1, 0);
  }
}

function invoke_iiiiiiijiii(index,a1,a2,a3,a4,a5,a6,a7,a8,a9,a10,a11) {
  var sp = stackSave();
  try {
    return dynCall_iiiiiiijiii(index,a1,a2,a3,a4,a5,a6,a7,a8,a9,a10,a11);
  } catch(e) {
    stackRestore(sp);
    if (e !== e+0) throw e;
    _setThrew(1, 0);
  }
}

function invoke_iiiiiiji(index,a1,a2,a3,a4,a5,a6,a7,a8) {
  var sp = stackSave();
  try {
    return dynCall_iiiiiiji(index,a1,a2,a3,a4,a5,a6,a7,a8);
  } catch(e) {
    stackRestore(sp);
    if (e !== e+0) throw e;
    _setThrew(1, 0);
  }
}

function invoke_jii(index,a1,a2) {
  var sp = stackSave();
  try {
    return dynCall_jii(index,a1,a2);
  } catch(e) {
    stackRestore(sp);
    if (e !== e+0) throw e;
    _setThrew(1, 0);
  }
}

function invoke_iiiji(index,a1,a2,a3,a4,a5) {
  var sp = stackSave();
  try {
    return dynCall_iiiji(index,a1,a2,a3,a4,a5);
  } catch(e) {
    stackRestore(sp);
    if (e !== e+0) throw e;
    _setThrew(1, 0);
  }
}

function invoke_iiiiiji(index,a1,a2,a3,a4,a5,a6,a7) {
  var sp = stackSave();
  try {
    return dynCall_iiiiiji(index,a1,a2,a3,a4,a5,a6,a7);
  } catch(e) {
    stackRestore(sp);
    if (e !== e+0) throw e;
    _setThrew(1, 0);
  }
}

function invoke_viji(index,a1,a2,a3,a4) {
  var sp = stackSave();
  try {
    dynCall_viji(index,a1,a2,a3,a4);
  } catch(e) {
    stackRestore(sp);
    if (e !== e+0) throw e;
    _setThrew(1, 0);
  }
}




// === Auto-generated postamble setup entry stuff ===

Module["ccall"] = ccall;
Module["cwrap"] = cwrap;
Module["UTF8ArrayToString"] = UTF8ArrayToString;
Module["UTF8ToString"] = UTF8ToString;
Module["addRunDependency"] = addRunDependency;
Module["removeRunDependency"] = removeRunDependency;
Module["FS_createPath"] = FS.createPath;
Module["FS_createDataFile"] = FS.createDataFile;
Module["FS_createPreloadedFile"] = FS.createPreloadedFile;
Module["FS_createLazyFile"] = FS.createLazyFile;
Module["FS_createDevice"] = FS.createDevice;
Module["FS_unlink"] = FS.unlink;
Module["print"] = out;
Module["setValue"] = setValue;
Module["getValue"] = getValue;
Module["FS"] = FS;

var calledRun;

/**
 * @constructor
 * @this {ExitStatus}
 */
function ExitStatus(status) {
  this.name = "ExitStatus";
  this.message = "Program terminated with exit(" + status + ")";
  this.status = status;
}

var calledMain = false;

dependenciesFulfilled = function runCaller() {
  // If run has never been called, and we should call run (INVOKE_RUN is true, and Module.noInitialRun is not false)
  if (!calledRun) run();
  if (!calledRun) dependenciesFulfilled = runCaller; // try this again later, after new deps are fulfilled
};

/** @type {function(Array=)} */
function run(args) {
  args = args || arguments_;

  if (runDependencies > 0) {
    return;
  }

  preRun();

  // a preRun added a dependency, run will be called later
  if (runDependencies > 0) {
    return;
  }

  function doRun() {
    // run may have just been called through dependencies being fulfilled just in this very frame,
    // or while the async setStatus time below was happening
    if (calledRun) return;
    calledRun = true;
    Module['calledRun'] = true;

    if (ABORT) return;

    initRuntime();

    readyPromiseResolve(Module);
    if (Module['onRuntimeInitialized']) Module['onRuntimeInitialized']();

    postRun();
  }

  if (Module['setStatus']) {
    Module['setStatus']('Running...');
    setTimeout(function() {
      setTimeout(function() {
        Module['setStatus']('');
      }, 1);
      doRun();
    }, 1);
  } else
  {
    doRun();
  }
}
Module['run'] = run;

/** @param {boolean|number=} implicit */
function exit(status, implicit) {
  EXITSTATUS = status;

  procExit(status);
}

function procExit(code) {
  EXITSTATUS = code;
  if (!keepRuntimeAlive()) {
    if (Module['onExit']) Module['onExit'](code);
    ABORT = true;
  }
  quit_(code, new ExitStatus(code));
}

if (Module['preInit']) {
  if (typeof Module['preInit'] == 'function') Module['preInit'] = [Module['preInit']];
  while (Module['preInit'].length > 0) {
    Module['preInit'].pop()();
  }
}

run();





createDotnetRuntime.ready = createDotnetRuntime.ready.then(() => { return __dotnet_exportedAPI; });

  return createDotnetRuntime.ready
}
);
})();
export default createDotnetRuntime;
const MONO = {}, BINDING = {}, INTERNAL = {}, IMPORTS = {};

// TODO duplicated from emscripten, so we can use them in the __setEmscriptenEntrypoint
var ENVIRONMENT_IS_WEB = typeof window == 'object';
var ENVIRONMENT_IS_WORKER = typeof importScripts == 'function';
var ENVIRONMENT_IS_NODE = typeof process == 'object' && typeof process.versions == 'object' && typeof process.versions.node == 'string';
var ENVIRONMENT_IS_SHELL = !ENVIRONMENT_IS_WEB && !ENVIRONMENT_IS_NODE && !ENVIRONMENT_IS_WORKER;

__dotnet_runtime.__setEmscriptenEntrypoint(createDotnetRuntime, { isNode: ENVIRONMENT_IS_NODE, isShell: ENVIRONMENT_IS_SHELL, isWeb: ENVIRONMENT_IS_WEB, isWorker: ENVIRONMENT_IS_WORKER });
const dotnet = __dotnet_runtime.moduleExports.dotnet;
const exit = __dotnet_runtime.moduleExports.exit;
export { dotnet, exit, INTERNAL };
